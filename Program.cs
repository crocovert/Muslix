
// ============================================================
//  MUSLIC — Version optimisée et parallélisée
// ============================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Muslic
{
    // ══════════════════════════════════════════════════════════════════════
    //  CLASSES DE DONNÉES DE BASE
    // ══════════════════════════════════════════════════════════════════════

    public class node
    {
        public string i = "", texte = "";
        public float x, y;
        public bool is_visible;
        public List<int> pred = new List<int>(), succ = new List<int>();
    }

    public class Service
    {
        public int numero, regime;
        public float hd, hf, volau, boat, alit;
    }

    public class link
    {
        public int no, nd, ligne;
        public string i = "", j = "", type = "", texte = "";
        public float d, t0, f, c, tmap;
        public List<Service> services = new List<Service>();
        // Accumulateurs globaux (mis à jour à la fin du calcul parallèle)
        public float volau, boai, alij;
    }

    public class network
    {
        public string nom = "";
        public List<node> nodes = new List<node>();
        public List<link> links = new List<link>();
        public Dictionary<string, int> numnoeud = new Dictionary<string, int>();
        public Dictionary<string, int> num_calendrier = new Dictionary<string, int>();
        public List<string> nom_calendrier = new List<string>();
    }

    public class PaireOD
    {
        public string p, q, libod;
        public float od, horaire;
        public int jour, sens, numod;
        public Dictionary<string, float> cveh, cwait, cmap, cboa, coef_tmap, tboa, tboa_max, ctoll;
        public bool sortie_chemins, a_params_specifiques;
        public int sortie_temps, algorithme;
        public float param_dijkstra, max_nb_buckets, pu;
        public string texte_filtre_sortie = "";
    }

    public class Param_affectation_horaire
    {
        public string nom_reseau = "", nom_matrice = "", nom_sortie = "", nom_penalites = "";
        public float param_dijkstra, pu, max_nb_buckets = 10000f, temps_max = 120f;
        public bool sortie_chemins, demitours = true, sortie_services, sortie_turns, test_OK;
        public int sortie_temps, algorithme = 1, nb_jours;
        public Dictionary<string, float> cveh = new Dictionary<string, float>(),
                                         cwait = new Dictionary<string, float>(),
                                         cmap = new Dictionary<string, float>(),
                                         cboa = new Dictionary<string, float>(),
                                         coef_tmap = new Dictionary<string, float>(),
                                         tboa = new Dictionary<string, float>(),
                                         tboa_max = new Dictionary<string, float>(),
                                         ctoll = new Dictionary<string, float>();
        public string texte_filtre_sortie = "";
    }

    public class Turn
    {
        public int arci, arcj;
        public override bool Equals(object o) => o is Turn t && arci == t.arci && arcj == t.arcj;
        public override int GetHashCode() => arci.GetHashCode() ^ arcj.GetHashCode();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ÉTAT DE CALCUL (Isolé par Thread)
    // ══════════════════════════════════════════════════════════════════════

    public class DijkstraState
    {
        public float[] cout, tatt, tcor, tmap, tveh, ttoll;
        public int[] touche, pivot, turn_pivot, service, gen;
        public List<List<int>> buckets;
        public int generation = 0;
        public int nb_pop = 0;

        public DijkstraState(int nbLinks, int nbBuckets)
        {
            cout = new float[nbLinks]; tatt = new float[nbLinks];
            tcor = new float[nbLinks]; tmap = new float[nbLinks];
            tveh = new float[nbLinks]; ttoll = new float[nbLinks];
            touche = new int[nbLinks]; pivot = new int[nbLinks];
            turn_pivot = new int[nbLinks]; service = new int[nbLinks];
            gen = new int[nbLinks];
            Array.Fill(gen, -1);
            buckets = new List<List<int>>(nbBuckets);
            for (int i = 0; i < nbBuckets; i++) buckets.Add(new List<int>(64));
        }

        public void Reset()
        {
            generation++;
            foreach (var b in buckets) b.Clear();
            nb_pop = 0;
        }

        public void EnsureInit(int idx)
        {
            if (gen[idx] == generation) return;
            cout[idx] = 1e38f; touche[idx] = 0; pivot[idx] = -1;
            gen[idx] = generation;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  LOGIQUE PRINCIPALE
    // ══════════════════════════════════════════════════════════════════════

    public class Program
    {
        public static void Main(string[] args)
        {
            // Point d'entrée simulé - L'utilisateur appelle affectation_tc
            Console.WriteLine("MUSLIC Parallel Engine Ready.");
        }

        public static async Task AffectationParallelAsync(network res, List<PaireOD> paires, Param_affectation_horaire aff, Dictionary<Turn, float> turns)
        {
            // 1. Groupement par Scénario Source (Origine ou Destination selon sens)
            var groupes = paires.GroupBy(od => od.sens == 1
                ? (Source: od.p, od.jour, od.horaire, od.sens)
                : (Source: od.q, od.jour, od.horaire, od.sens)).ToList();

            int nbLinks = res.links.Count;
            int maxBuckets = (int)aff.max_nb_buckets;

            // 2. Préparation du pool d'états
            using var statePool = new ThreadLocal<DijkstraState>(() => new DijkstraState(nbLinks, maxBuckets));

            // 3. Channel pour les sorties fichiers (Non-bloquant)
            var odOutput = Channel.CreateUnbounded<string>();
            var fileWriterTask = Task.Run(async () =>
            {
                using var sw = new StreamWriter(aff.nom_sortie + "_od.txt");
                await foreach (var line in odOutput.Reader.ReadAllAsync())
                {
                    await sw.WriteLineAsync(line);
                }
            });

            // 4. Boucle Parallèle
            Parallel.ForEach(groupes, grp =>
            {
                var st = statePool.Value;
                st.Reset();

                // On lance l'algorithme de Dijkstra pour la racine du groupe
                if (grp.Key.sens == 1)
                    CalculerDijkstraSens1(grp.Key.Source, grp.Key.jour, grp.Key.horaire, res, st, turns, aff);
                else
                    CalculerDijkstraSens2(grp.Key.Source, grp.Key.jour, grp.Key.horaire, res, st, turns, aff);

                // Extraction des résultats pour chaque OD du groupe
                foreach (var od in grp)
                {
                    string cible = (od.sens == 1) ? od.q : od.p;
                    if (res.numnoeud.TryGetValue(cible, out int nodeIdx))
                    {
                        // On écrit le résultat dans le channel
                        float finalCost = st.cout[nodeIdx];
                        odOutput.Writer.TryWrite($"{od.p};{od.q};{od.jour};{od.horaire};{finalCost}");
                    }
                }
            });

            // 5. Finalisation
            odOutput.Writer.Complete();
            await fileWriterTask;
        }

        // --- Méthodes Dijkstra (Placeholders à remplir avec votre logique exacte) ---

        private static void CalculerDijkstraSens1(string origin, int jour, float heure, network res, DijkstraState st, Dictionary<Turn, float> turns, Param_affectation_horaire p)
        {
            // Ici, vous insérez votre boucle 'while' de Dijkstra originale.
            // IMPORTANT: Remplacez 'link.cout' par 'st.cout[linkIdx]' pour être thread-safe.
        }

        private static void CalculerDijkstraSens2(string dest, int jour, float heure, network res, DijkstraState st, Dictionary<Turn, float> turns, Param_affectation_horaire p)
        {
            // Même chose pour le sens inverse.
        }
    }
}
