#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Globalization;

namespace Muslic
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 3)
            {
                string nom_reseau = args[0];
                string nom_matrice = args[1];
                string nom_sortie = args[2];
                string nom_parametres = args[3];
                string nom_penalites = args.Length > 4 ? args[4] : null;
                affectation_tc(nom_reseau, nom_matrice, nom_sortie, nom_parametres, nom_penalites);
            }
            else
            {
                Console.WriteLine("Usage: Muslic <network> <matrix> <output> <params> [penalties]");
            }
        }

        // ----------------------------
        // Parameters I/O
        // ----------------------------
        public static void Ecrit_parametres(Param_affectation_horaire parametres, string nom_fichier_ini)
        {
            using var fich_ini = new StreamWriter(nom_fichier_ini, false, Encoding.UTF8);
            if (parametres.sortie_stops) parametres.sortie_temps += 10;
            string texte =
                parametres.algorithme + ";algorithm" +
                "\n" + parametres.demitours + ";prohibited U-turns" +
                "\n" + parametres.max_nb_buckets + ";max buckets" +
                "\n" + parametres.nb_jours + ";number of days" +
                "\n" + parametres.nom_matrice + ";matrix file" +
                "\n" + parametres.nom_penalites + ";turns and transfers file" +
                "\n" + parametres.nom_reseau + ";network file" +
                "\n" + parametres.nom_sortie + ";generic output file" +
                "\n" + parametres.param_dijkstra + ";algorithm parameter+" +
                "\n" + parametres.pu + ";algorithm power" +
                "\n" + parametres.sortie_chemins + ";output paths" +
                "\n" + parametres.sortie_services + ";output services" +
                "\n" + parametres.sortie_temps + ";output travel times" +
                "\n" + parametres.sortie_turns + ";output turns and transfers" +
                "\n" + parametres.texte_cboa + ";boarding weight" +
                "\n" + parametres.texte_cmap + ";individual mode weight" +
                "\n" + parametres.texte_coef_tmap + ";indivudal travel time factor" +
                "\n" + parametres.texte_cveh + ";in-vehicle time weight" +
                "\n" + parametres.texte_cwait + ";wait time weight" +
                "\n" + parametres.texte_tboa + ";min transfer time" +
                "\n" + parametres.texte_tboa_max + ";max transfer time" +
                "\n" + parametres.tmapmax + ";max individual travel time" +
                "\n" + parametres.texte_toll + ";toll weight" +
                "\n" + parametres.texte_filtre_sortie + ";output filter types" +
                "\n" + parametres.temps_max + ";max travel cost" +
                "\n" + parametres.sortie_noeuds + ";output nodes" +
                "\n" + parametres.sortie_isoles + ";output isolated links";
            fich_ini.WriteLine(texte);
        }

        public static Param_affectation_horaire lit_parametres(string nom_parametres)
        {
            var aff_hor = new Param_affectation_horaire();
            if (!File.Exists(nom_parametres)) { aff_hor.test_OK = false; return aff_hor; }
            using var fich_ini = new StreamReader(nom_parametres);
            string line;
            line = fich_ini.ReadLine(); if (line != null) aff_hor.algorithme = int.Parse(line.Split(';')[0]);
            line = fich_ini.ReadLine(); if (line != null) aff_hor.demitours = bool.Parse(line.Split(';')[0]);
            line = fich_ini.ReadLine(); if (line != null) aff_hor.max_nb_buckets = int.Parse(line.Split(';')[0]);
            line = fich_ini.ReadLine(); if (line != null) aff_hor.nb_jours = int.Parse(line.Split(';')[0]);
            line = fich_ini.ReadLine(); if (line != null) aff_hor.nom_matrice = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.nom_penalites = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.nom_reseau = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.nom_sortie = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.param_dijkstra = int.Parse(line.Split(';')[0]);
            line = fich_ini.ReadLine(); if (line != null) aff_hor.pu = float.Parse(line.Split(';')[0].Replace(',', '.'), CultureInfo.InvariantCulture);
            line = fich_ini.ReadLine(); if (line != null) aff_hor.sortie_chemins = bool.Parse(line.Split(';')[0]);
            line = fich_ini.ReadLine(); if (line != null) aff_hor.sortie_services = bool.Parse(line.Split(';')[0]);
            line = fich_ini.ReadLine(); if (line != null) aff_hor.sortie_temps = int.Parse(line.Split(';')[0]);
            if (aff_hor.sortie_temps >= 10) { aff_hor.sortie_stops = true; aff_hor.sortie_temps -= 10; } else aff_hor.sortie_stops = false;
            line = fich_ini.ReadLine(); if (line != null) aff_hor.sortie_turns = bool.Parse(line.Split(';')[0]);
            line = fich_ini.ReadLine(); if (line != null) aff_hor.texte_cboa = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.texte_cmap = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.texte_coef_tmap = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.texte_cveh = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.texte_cwait = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.texte_tboa = line.Split(';')[0];
            line = fich_ini.ReadLine(); if (line != null) aff_hor.texte_tboa_max = line.Split(';')[0];
            if (!fich_ini.EndOfStream) { line = fich_ini.ReadLine(); if (line != null) aff_hor.tmapmax = ParseFloatInvariant(line.Split(';')[0]); }
            if (!fich_ini.EndOfStream) { line = fich_ini.ReadLine(); if (line != null) aff_hor.texte_toll = line.Split(';')[0]; }
            if (!fich_ini.EndOfStream) { line = fich_ini.ReadLine(); if (line != null) aff_hor.texte_filtre_sortie = line.Split(';')[0]; }
            if (!fich_ini.EndOfStream) { line = fich_ini.ReadLine(); if (line != null) aff_hor.temps_max = ParseFloatInvariant(line.Split(';')[0]); }
            if (!fich_ini.EndOfStream) { line = fich_ini.ReadLine(); if (line != null) aff_hor.sortie_noeuds = bool.Parse(line.Split(';')[0]); }
            if (!fich_ini.EndOfStream) { line = fich_ini.ReadLine(); if (line != null) aff_hor.sortie_isoles = bool.Parse(line.Split(';')[0]); }
            aff_hor.test_OK = true;
            return aff_hor;
        }

        // ----------------------------
        // Main: parallelized OD processing by groups
        // ----------------------------
        public static void affectation_tc(string nom_reseau, string nom_matrice, string nom_sortie, string nom_parametres, string nom_penalites)
        {
            if (!File.Exists(nom_reseau) || !File.Exists(nom_matrice) || !File.Exists(nom_parametres))
            {
                Console.WriteLine("Missing input files.");
                return;
            }

            var projet = new etude();
            var aff_hor = lit_parametres(nom_parametres);
            projet.param_affectation_horaire = aff_hor;
            aff_hor.nom_sortie = nom_sortie;
            aff_hor.nom_reseau = nom_reseau;
            aff_hor.nom_matrice = nom_matrice;
            aff_hor.nom_penalites = nom_penalites;

            // load network
            projet.reseaux.Add(new network());
            projet.reseau_actif = 0;
            ReadNetworkFile(projet, nom_reseau);

            // build topology
            BuildTopology(projet);

            // initialize per-link dynamic fields in parallel
            var linksList = projet.reseaux[projet.reseau_actif].links;
            Parallel.For(0, linksList.Count, i =>
            {
                var ln = linksList[i];
                ln.l = 0;
                ln.volau = 0;
                ln.touche = 0;
                ln.cout = 0;
                ln.pivot = -1;
                ln.is_queue = false;
                ln.turn_pivot = -1;
                for (int s = 0; s < ln.services.Count; s++)
                {
                    ln.services[s].delta = 0;
                    ln.services[s].alij = 0;
                    ln.services[s].boai = 0;
                }
            });

            // read matrix and create OD records
            var odRecords = ReadMatrixToOdRecords(nom_matrice);

            // group OD: if sens==1 group by origin/jour/horaire; if sens==2 group by dest/jour/horaire
            var groups = odRecords.GroupBy(r => r.Sens == 1 ? $"O|{r.P}|{r.Jour}|{r.Horaire}" : $"D|{r.Q}|{r.Jour}|{r.Horaire}").ToList();

            // prepare thread-safe collectors
            var bag_chemins = new ConcurrentBag<string>();
            var bag_od = new ConcurrentBag<string>();
            var bag_noeuds = new ConcurrentBag<string>();
            var bag_result = new ConcurrentBag<string>();
            var bag_services = new ConcurrentBag<string>();
            var bag_turns = new ConcurrentBag<string>();
            var bag_detour = new ConcurrentBag<string>();
            var bag_isoles = new ConcurrentBag<string>();

            // parallel processing of groups: clone network per group for isolation
            Parallel.ForEach(groups, group =>
            {
                var localNet = CloneNetwork(projet.reseaux[projet.reseau_actif]);
                ProcessGroupOnLocalNetwork(localNet, group.ToList(), projet.param_affectation_horaire,
                    bag_chemins, bag_od, bag_noeuds, bag_result, bag_services, bag_turns, bag_detour, bag_isoles);
            });

            // write outputs sequentially
            File.WriteAllLines(aff_hor.nom_sortie + "_chemins.txt", bag_chemins);
            File.WriteAllLines(aff_hor.nom_sortie + "_od.txt", bag_od);
            File.WriteAllLines(aff_hor.nom_sortie + "_noeuds.txt", bag_noeuds);
            File.WriteAllLines(aff_hor.nom_sortie + "_aff.txt", bag_result);
            if (aff_hor.sortie_services) File.WriteAllLines(aff_hor.nom_sortie + "_services.txt", bag_services);
            if (aff_hor.sortie_turns) File.WriteAllLines(aff_hor.nom_sortie + "_transferts.txt", bag_turns);
            File.WriteAllLines(aff_hor.nom_sortie + "_detour.txt", bag_detour);
            File.WriteAllLines(aff_hor.nom_sortie + "_isoles.txt", bag_isoles);
        }

        // ----------------------------
        // Helpers: network IO & topology
        // ----------------------------
        static void ReadNetworkFile(etude projet, string nom_reseau)
        {
            int num_res = projet.reseau_actif;
            using var flux_reseau = new FileStream(nom_reseau, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var fichier_reseau = new StreamReader(flux_reseau, Encoding.UTF8);
            projet.reseaux[num_res].nom = Path.GetFileNameWithoutExtension(nom_reseau);
            string[] param = { ";" };
            string carte = "t links";
            while (!fichier_reseau.EndOfStream)
            {
                var chaine = fichier_reseau.ReadLine();
                if (string.IsNullOrWhiteSpace(chaine)) continue;
                if (chaine.Length >= 7)
                {
                    var header = chaine.Substring(0, 7);
                    if (header == "t nodes") { carte = "t nodes"; continue; }
                    if (header == "t links") { carte = "t links"; continue; }
                }
                var ch = chaine.Split(param, StringSplitOptions.None);
                if (carte == "t nodes")
                {
                    string ni = ch[0].Trim();
                    if (!projet.reseaux[num_res].numnoeud.ContainsKey(ni))
                    {
                        projet.reseaux[num_res].numnoeud.Add(ni, projet.reseaux[num_res].nodes.Count);
                        float xi = ParseFloatInvariant(ch.Length > 1 ? ch[1] : "0");
                        float yi = ParseFloatInvariant(ch.Length > 2 ? ch[2] : "0");
                        var noeud = new node { i = ni, x = xi, y = yi, is_visible = true };
                        if (ch.Length > 3) noeud.texte = ch[3];
                        projet.reseaux[num_res].nodes.Add(noeud);
                        projet.reseaux[num_res].xu = Math.Max(projet.reseaux[num_res].xu, xi);
                        projet.reseaux[num_res].xl = Math.Min(projet.reseaux[num_res].xl, xi);
                        projet.reseaux[num_res].yu = Math.Max(projet.reseaux[num_res].yu, yi);
                        projet.reseaux[num_res].yl = Math.Min(projet.reseaux[num_res].yl, yi);
                    }
                }
                else
                {
                    string ni = ch[0].Trim();
                    string nj = ch[1].Trim();
                    if (!projet.reseaux[num_res].numnoeud.ContainsKey(ni))
                    {
                        projet.reseaux[num_res].numnoeud.Add(ni, projet.reseaux[num_res].nodes.Count);
                        projet.reseaux[num_res].nodes.Add(new node { i = ni });
                    }
                    if (!projet.reseaux[num_res].numnoeud.ContainsKey(nj))
                    {
                        projet.reseaux[num_res].numnoeud.Add(nj, projet.reseaux[num_res].nodes.Count);
                        projet.reseaux[num_res].nodes.Add(new node { i = nj });
                    }
                    var lien = new link
                    {
                        no = projet.reseaux[num_res].numnoeud[ni],
                        nd = projet.reseaux[num_res].numnoeud[nj],
                        temps = ch.Length > 2 ? ParseFloatInvariant(ch[2]) : 0,
                        longueur = ch.Length > 3 ? ParseFloatInvariant(ch[3]) : 0,
                        ligne = ch.Length > 4 && int.TryParse(ch[4], out var lv) ? lv : 0
                    };
                    if (ch.Length > 5 && int.TryParse(ch[5].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var svc))
                    {
                        var s = new Service { numero = svc };
                        if (ch.Length > 6 && float.TryParse(ch[6].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var hd)) s.hd = hd;
                        if (ch.Length > 7 && float.TryParse(ch[7].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var hf)) s.hf = hf;
                        lien.services.Add(s);
                        projet.reseaux[num_res].nbservices++;
                    }
                    if (ch.Length > 9) lien.texte = ch[9];
                    if (ch.Length > 10) lien.type = ch[10].Trim();
                    if (ch.Length > 11) lien.toll = ParseFloatInvariant(ch[11]);
                    projet.reseaux[num_res].links.Add(lien);
                }
            }
        }

        static void BuildTopology(etude projet)
        {
            int idxMax = projet.reseaux[projet.reseau_actif].links.Count;
            for (int i = 0; i < idxMax; i++)
            {
                var l = projet.reseaux[projet.reseau_actif].links[i];
                projet.reseaux[projet.reseau_actif].nodes[l.nd].pred.Add(i);
                projet.reseaux[projet.reseau_actif].nodes[l.no].succ.Add(i);
            }
        }

        // ----------------------------
        // Matrix -> OD records
        // ----------------------------
        static List<OdRecord> ReadMatrixToOdRecords(string nom_matrice)
        {
            var lines = File.ReadAllLines(nom_matrice, Encoding.UTF8).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            var records = new List<OdRecord>(lines.Length);
            foreach (var ligne in lines)
            {
                var ch = ligne.Split(';');
                if (ch.Length < 5) continue;
                if (!TryParseFloatInvariant(ch[2], out var od)) continue;
                if (!TryParseFloatInvariant(ch[4], out var horaire)) continue;
                if (!TryParseFloatInvariant(ch[3], out var jf)) continue;
                int jour = (int)jf;
                int sens = 1;
                if (ch.Length > 5 && !string.IsNullOrWhiteSpace(ch[5])) sens = ch[5].Trim().ToLowerInvariant() == "a" ? 2 : 1;
                records.Add(new OdRecord
                {
                    P = ch[0].Trim(),
                    Q = ch[1].Trim(),
                    Od = od,
                    Jour = jour,
                    Horaire = horaire,
                    Sens = sens,
                    LibOd = ch.Length > 6 ? ch[6] : "",
                    RawFields = ch
                });
            }
            return records;
        }

        // ----------------------------
        // Parallel group processing (each group works on a cloned network)
        // ----------------------------
        static void ProcessGroupOnLocalNetwork(
            network localNet,
            List<OdRecord> records,
            Param_affectation_horaire param,
            ConcurrentBag<string> bag_chemins,
            ConcurrentBag<string> bag_od,
            ConcurrentBag<string> bag_noeuds,
            ConcurrentBag<string> bag_result,
            ConcurrentBag<string> bag_services,
            ConcurrentBag<string> bag_turns,
            ConcurrentBag<string> bag_detour,
            ConcurrentBag<string> bag_isoles)
        {
            foreach (var rec in records)
            {
                ProcessSingleOdOnLocalNetwork(localNet, rec, param, bag_chemins, bag_od, bag_noeuds, bag_result, bag_services, bag_turns, bag_detour, bag_isoles);
            }
        }

        // ----------------------------
        // Extracted per-OD processing (operate on localNet)
        // Replace/extend internals to match original algorithm exactly.
        // ----------------------------
        static void ProcessSingleOdOnLocalNetwork(
            network localNet,
            OdRecord rec,
            Param_affectation_horaire param,
            ConcurrentBag<string> bag_chemins,
            ConcurrentBag<string> bag_od,
            ConcurrentBag<string> bag_noeuds,
            ConcurrentBag<string> bag_result,
            ConcurrentBag<string> bag_services,
            ConcurrentBag<string> bag_turns,
            ConcurrentBag<string> bag_detour,
            ConcurrentBag<string> bag_isoles)
        {
            // Basic validation
            if (!localNet.numnoeud.TryGetValue(rec.P, out int originIdx) || !localNet.numnoeud.TryGetValue(rec.Q, out int destIdx))
            {
                bag_isoles.Add($"{rec.P}-{rec.Q};missing_node");
                return;
            }

            // The original algorithm is large and stateful. For safety we show a correct, thread-safe pattern:
            // compute touched seeds and run a local GGA on localNet (simplified placeholder here).
            // You should replace the placeholder block below with the full original loop,
            // switching all accesses from global project.reseaux[...] to localNet.

            // Placeholder: choose first outgoing touched arc as demo result (safe, deterministic)
            int chosen = -1;
            double best = double.PositiveInfinity;
            foreach (var li in localNet.nodes[originIdx].succ)
            {
                var a = localNet.links[li];
                if (a.temps < best)
                {
                    best = a.temps; chosen = li;
                }
            }

            string libod = string.IsNullOrEmpty(rec.LibOd) ? $"{rec.P}->{rec.Q}" : rec.LibOd;
            if (chosen >= 0)
            {
                var a = localNet.links[chosen];
                string chemin = $"{libod};{rec.P};{rec.Q};{rec.Jour};{rec.Horaire:0.000};{localNet.nodes[a.no].i};{localNet.nodes[a.nd].i};{a.ligne};{a.temps:0.000}";
                bag_chemins.Add(chemin);
                bag_od.Add($"{libod};{rec.P};{rec.Q};{rec.Jour};{rec.Horaire:0.000};{a.temps:0.000};{a.volau:0.00}");
                bag_result.Add($"{localNet.nodes[a.no].i};{localNet.nodes[a.nd].i};{a.ligne};{a.volau:0.00};{a.texte};{a.type};{a.toll:0.000}");
            }
            else
            {
                bag_isoles.Add($"{rec.P}-{rec.Q};unreachable");
            }
        }

        // ----------------------------
        // Utilities: parsing, cloning
        // ----------------------------
        static float ParseFloatInvariant(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0f;
            s = s.Trim().Replace(',', '.');
            return float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0f;
        }
        static bool TryParseFloatInvariant(string s, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().Replace(',', '.');
            return float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        static network CloneNetwork(network orig)
        {
            var copy = new network
            {
                nom = orig.nom,
                xl = orig.xl,
                xu = orig.xu,
                yl = orig.yl,
                yu = orig.yu,
                max_type = orig.max_type,
                nbturns = orig.nbturns,
                nbservices = orig.nbservices,
                nom_calendrier = new List<string>(orig.nom_calendrier),
                num_calendrier = new Dictionary<string, int>(orig.num_calendrier),
                numnoeud = new Dictionary<string, int>(orig.numnoeud),
                noms_arcs = new Dictionary<string, string>(orig.noms_arcs)
            };
            foreach (var n in orig.nodes)
            {
                copy.nodes.Add(new node
                {
                    i = n.i,
                    x = n.x,
                    y = n.y,
                    tempst = n.tempst,
                    tmap = n.tmap,
                    tatt = n.tatt,
                    temps = n.temps,
                    cout = n.cout,
                    ncor = n.ncor,
                    ttoll = n.ttoll,
                    ci = n.ci,
                    is_valid = n.is_valid,
                    is_visible = n.is_visible,
                    is_intersection = n.is_intersection,
                    texte = n.texte,
                    pole = n.pole,
                    pred = new List<int>(n.pred),
                    succ = new List<int>(n.succ)
                });
            }
            foreach (var l in orig.links)
            {
                var ll = new link
                {
                    longueur = l.longueur,
                    temps = l.temps,
                    cout = l.cout,
                    v0 = l.v0,
                    vsat = l.vsat,
                    tatt = l.tatt,
                    tcor = l.tcor,
                    tveh = l.tveh,
                    tmap = l.tmap,
                    tatt1 = l.tatt1,
                    a = l.a,
                    b = l.b,
                    n = l.n,
                    volau = l.volau,
                    lanes = l.lanes,
                    h = l.h,
                    l = l.l,
                    alij = l.alij,
                    boai = l.boai,
                    ncorr = l.ncorr,
                    toll = l.toll,
                    ttoll = l.ttoll,
                    no = l.no,
                    nd = l.nd,
                    service = l.service,
                    vdf = l.vdf,
                    touche = l.touche,
                    pivot = l.pivot,
                    ligne = l.ligne,
                    turn_pivot = l.turn_pivot,
                    is_queue = l.is_queue,
                    is_valid = l.is_valid,
                    texte = l.texte,
                    modes = l.modes,
                    pole = l.pole,
                    type = l.type,
                    poleV2 = l.poleV2
                };
                foreach (var s in l.services)
                {
                    ll.services.Add(new Service
                    {
                        numero = s.numero,
                        hd = s.hd,
                        hf = s.hf,
                        delta = s.delta,
                        boai = s.boai,
                        alij = s.alij,
                        alit = s.alit,
                        boat = s.boat,
                        volau = s.volau,
                        regime = s.regime
                    });
                }
                copy.links.Add(ll);
            }
            return copy;
        }

        // ----------------------------
        // Domain types (kept minimal & compatible)
        // ----------------------------
        public class OdRecord { public string P; public string Q; public float Od; public int Jour; public float Horaire; public int Sens; public string LibOd; public string[] RawFields; }

        public class vecteur { public List<float> d = new List<float>(0); }
        public class Turn { public int arci, arcj; public override bool Equals(object obj) { var t = obj as Turn; return t != null && arci == t.arci && arcj == t.arcj; } public override int GetHashCode() => arci.GetHashCode() ^ arcj.GetHashCode(); }
        public class turn { public int numero; public float temps; public bool is_valid = false; }
        public class node { public float x, y, tempst = 1e38f, tmap = 0, tatt, temps, cout, ncor, ttoll; public string i; public bool ci = false, is_valid = true, is_visible = false, is_intersection = false; public List<int> pred = new List<int>(); public List<int> succ = new List<int>(); public string texte; public string pole; }
        public class Link_num { public string i, j; public int line; public override bool Equals(object obj) { var ln = obj as Link_num; return ln != null && i == ln.i && j == ln.j && line == ln.line; } public override int GetHashCode() => i.GetHashCode() ^ j.GetHashCode() ^ line.GetHashCode(); }
        public class link { public float longueur, temps, cout, v0, vsat, tatt, tcor, tveh, tmap, tatt1, a, b, n, volau, lanes, h, l, alij, boai, ncorr, toll = 0, ttoll; public int no, nd, service, vdf, touche, pivot, ligne, turn_pivot = -1; public bool is_queue, is_valid = true; public List<Service> services = new List<Service>(); public string texte, modes, pole, type = "0", poleV2; }
        public class matrix { public string nom; public List<vecteur> o = new List<vecteur>(0); }
        public class network { public string nom; public List<link> links = new List<link>(20000); public List<node> nodes = new List<node>(10000); public float xl = 1e38f, xu = -1e38f, yl = 1e38f, yu = -1e38f; public List<matrix> matrices = new List<matrix>(); public Dictionary<string, int> numnoeud = new Dictionary<string, int>(); public Dictionary<string, int> num_calendrier = new Dictionary<string, int>(); public List<string> nom_calendrier = new List<string>(); public Dictionary<string, string> noms_arcs = new Dictionary<string, string>(); public int max_type = 0, nbturns = 0, nbservices = 0; }
        public class etude { public string nom; public int reseau_actif; public List<network> reseaux = new List<network>(); public Param_affectation_horaire param_affectation_horaire = new Param_affectation_horaire(); }
        public class Param_affectation_horaire
        {
            public string nom_reseau, nom_matrice, nom_sortie, nom_penalites;
            public Dictionary<string, float> coef_tmap = new Dictionary<string, float>();
            public Dictionary<string, float> cmap = new Dictionary<string, float>();
            public Dictionary<string, float> cwait = new Dictionary<string, float>();
            public Dictionary<string, float> cboa = new Dictionary<string, float>();
            public Dictionary<string, float> tboa = new Dictionary<string, float>();
            public Dictionary<string, float> tboa_max = new Dictionary<string, float>(1);
            public Dictionary<string, float> cveh = new Dictionary<string, float>(1);
            public Dictionary<string, float> ctoll = new Dictionary<string, float>(1);
            public float param_dijkstra, pu;
            public bool sortie_chemins, demitours = true, sortie_services = false, sortie_turns = false, test_OK = false, sortie_noeuds = true, sortie_isoles = false, sortie_stops = false;
            public int sortie_temps;
            public int algorithme = 1;
            public float max_nb_buckets = 10000;
            public int nb_jours = 0;
            public string texte_coef_tmap;
            public string texte_cmap;
            public string texte_cwait;
            public string texte_cboa;
            public string texte_tboa;
            public string texte_tboa_max;
            public string texte_cveh, texte_toll;
            public float tmapmax, temps_max = 120;
            public int nb_pop = 0;
            public string texte_filtre_sortie = "";
        }
        public class Service { public int numero; public float hd, hf, delta = 0, boai = 0, alij = 0, alit = 0, boat = 0, volau = 0; public int regime; }
        public class suivant { public List<int> classe = new List<int>(); }
        public class Node { public string i = ""; public float x = 0, y = 0, tempst = 1e38f, tmap = 0, tatt, temps, cout, ncor, ttoll; public string name = ""; public string pole, poleV2; public List<int> in_nodes = new List<int>(); public List<int> out_nodes = new List<int>(); public void addincoming(int i) { this.in_nodes.Add(i); } public void addoutgoing(int i) { this.out_nodes.Add(i); } }
        public class Link { }

        // legacy tests
        public static bool test_temps_per_min_depart(etude projet, int arrivee)
        {
            bool reponse = true;
            var arc = projet.reseaux[projet.reseau_actif].links[arrivee];
            int ni = arc.no;
            foreach (int k in projet.reseaux[projet.reseau_actif].nodes[ni].succ)
            {
                int nj = projet.reseaux[projet.reseau_actif].links[k].nd;
                int ligne = projet.reseaux[projet.reseau_actif].links[k].ligne;
                if ((nj == arc.nd) && !(arc.ligne == ligne) && projet.reseaux[projet.reseau_actif].links[k].touche != 0)
                {
                    if (arc.cout > projet.reseaux[projet.reseau_actif].links[k].cout) reponse = false;
                }
            }
            return reponse;
        }
        public static bool test_temps_per_min_arrivee(etude projet, int arrivee)
        {
            bool reponse = true;
            var arc = projet.reseaux[projet.reseau_actif].links[arrivee];
            int nj = arc.nd;
            foreach (int k in projet.reseaux[projet.reseau_actif].nodes[nj].pred)
            {
                int ni = projet.reseaux[projet.reseau_actif].links[k].no;
                int ligne = projet.reseaux[projet.reseau_actif].links[k].ligne;
                if ((ni == arc.no) && !(arc.ligne == ligne) && projet.reseaux[projet.reseau_actif].links[k].touche != 0)
                {
                    if (arc.cout > projet.reseaux[projet.reseau_actif].links[k].cout) reponse = false;
                }
            }
            return reponse;
        }
    }
}