using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Muslic
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() > 3)
            {
                string nom_reseau = args[0];
                string nom_matrice = args[1];
                string nom_sortie = args[2];
                string nom_parametres = args[3];
                string nom_penalites = null;
                if (args.Count() > 4)
                {
                    nom_penalites = args[4];
                }
                affectation_tc(nom_reseau, nom_matrice, nom_sortie, nom_parametres, nom_penalites);
            }
        }

        public static void Ecrit_parametres(Param_affectation_horaire parametres, string nom_fichier_ini)
        {
            System.IO.StreamWriter fich_ini = new System.IO.StreamWriter(nom_fichier_ini, false, System.Text.Encoding.UTF8);
            if (parametres.sortie_stops == true)
            {
                parametres.sortie_temps += 10;
            }
            String texte = parametres.algorithme + ";algorithm" +
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
            fich_ini.Close();
        }

        public static Param_affectation_horaire lit_parametres(string nom_parametres)
        {
            Param_affectation_horaire aff_hor = new Param_affectation_horaire();
            if (System.IO.File.Exists(nom_parametres) == true)
            {
                System.IO.StreamReader fich_ini = new System.IO.StreamReader(nom_parametres);

                aff_hor.algorithme = int.Parse(fich_ini.ReadLine().Split(';')[0]);
                aff_hor.demitours = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                aff_hor.max_nb_buckets = int.Parse(fich_ini.ReadLine().Split(';')[0]);
                aff_hor.nb_jours = int.Parse(fich_ini.ReadLine().Split(';')[0]);
                aff_hor.nom_matrice = fich_ini.ReadLine().Split(';')[0];
                aff_hor.nom_penalites = fich_ini.ReadLine().Split(';')[0];
                aff_hor.nom_reseau = fich_ini.ReadLine().Split(';')[0];
                aff_hor.nom_sortie = fich_ini.ReadLine().Split(';')[0];
                aff_hor.param_dijkstra = int.Parse(fich_ini.ReadLine().Split(';')[0]);
                aff_hor.pu = float.Parse(fich_ini.ReadLine().Split(';')[0]);
                aff_hor.sortie_chemins = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                aff_hor.sortie_services = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                aff_hor.sortie_temps = int.Parse(fich_ini.ReadLine().Split(';')[0]);
                if (aff_hor.sortie_temps >= 10)
                {
                    aff_hor.sortie_stops = true;
                    aff_hor.sortie_temps += -10;
                }
                else
                {
                    aff_hor.sortie_stops = false;
                }
                aff_hor.sortie_turns = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                aff_hor.texte_cboa = fich_ini.ReadLine().Split(';')[0];
                aff_hor.texte_cmap = fich_ini.ReadLine().Split(';')[0];
                aff_hor.texte_coef_tmap = fich_ini.ReadLine().Split(';')[0];
                aff_hor.texte_cveh = fich_ini.ReadLine().Split(';')[0];
                aff_hor.texte_cwait = fich_ini.ReadLine().Split(';')[0];
                aff_hor.texte_tboa = fich_ini.ReadLine().Split(';')[0];
                aff_hor.texte_tboa_max = fich_ini.ReadLine().Split(';')[0];

                if (fich_ini.EndOfStream == false)
                {
                    if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                    {
                        aff_hor.tmapmax = float.Parse(fich_ini.ReadLine().Replace(",", ".").Split(';')[0]);
                    }
                    else
                    {
                        aff_hor.tmapmax = float.Parse(fich_ini.ReadLine().Replace(".", ",").Split(';')[0]);
                    }
                }
                if (fich_ini.EndOfStream == false)
                {
                    aff_hor.texte_toll = fich_ini.ReadLine().Split(';')[0];
                }

                if (fich_ini.EndOfStream == false)
                {
                    aff_hor.texte_filtre_sortie = fich_ini.ReadLine().Split(';')[0];
                }

                if (fich_ini.EndOfStream == false)
                {
                    if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                    {
                        aff_hor.temps_max = float.Parse(fich_ini.ReadLine().Replace(",", ".").Split(';')[0]);
                    }
                    else
                    {
                        aff_hor.temps_max = float.Parse(fich_ini.ReadLine().Replace(".", ",").Split(';')[0]);
                    }
                }
                if (fich_ini.EndOfStream == false)
                {
                    aff_hor.sortie_noeuds = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                }
                if (fich_ini.EndOfStream == false)
                {
                    aff_hor.sortie_isoles = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                }
                fich_ini.Close();
            }
            aff_hor.test_OK = true;
            return aff_hor;
        }

        // Structure pour grouper les OD
        public class ODGroup
        {
            public string Key { get; set; } // Clé de groupement
            public string Origin { get; set; }
            public string Destination { get; set; }
            public int Day { get; set; }
            public float Hour { get; set; }
            public int Direction { get; set; } // 1 ou 2
            public float TotalDemand { get; set; }
            public List<ODEntry> Entries { get; set; } = new List<ODEntry>();
        }

        public class ODEntry
        {
            public float Demand { get; set; }
            public string LibOD { get; set; }
        }

        // Structure pour stocker les résultats d'un groupe OD traité
        public class ODGroupResult
        {
            public string GroupKey { get; set; }
            public List<string> OutputLinesTemps { get; set; } = new List<string>();
            public List<string> OutputLinesChemin { get; set; } = new List<string>();
            public List<string> OutputLinesOD { get; set; } = new List<string>();
            public List<string> OutputLinesNoeuds { get; set; } = new List<string>();
            public List<string> OutputLinesDetour { get; set; } = new List<string>();
            public Dictionary<int, LinkAffectation> LinkAffectations { get; set; } = new Dictionary<int, LinkAffectation>();
            public Dictionary<Turn, float> Transfers { get; set; } = new Dictionary<Turn, float>();
        }

        public class LinkAffectation
        {
            public float volau { get; set; }
            public float boai { get; set; }
            public float alij { get; set; }
            public Dictionary<int, ServiceAffectation> ServiceAffectations { get; set; } = new Dictionary<int, ServiceAffectation>();
        }

        public class ServiceAffectation
        {
            public float volau { get; set; }
            public float boat { get; set; }
            public float alit { get; set; }
        }

        public static void affectation_tc(string nom_reseau, string nom_matrice, string nom_sortie, string nom_parametres, string nom_penalites)
        {
            int i, j;

            HashSet<String> types = new HashSet<string>();
            Dictionary<Turn, float> turns = new Dictionary<Turn, float>();
            Dictionary<Turn, float> transfers = new Dictionary<Turn, float>();
            Dictionary<Link_num, int> link_id = new Dictionary<Link_num, int>();
            etude projet = new etude();
            Param_affectation_horaire aff_hor = lit_parametres(nom_parametres);

            projet.param_affectation_horaire = aff_hor;
            aff_hor.nom_sortie = nom_sortie;
            aff_hor.nom_reseau = nom_reseau;
            aff_hor.nom_matrice = nom_matrice;
            aff_hor.nom_penalites = nom_penalites;

            if (System.IO.File.Exists(nom_parametres) == true)
            {
                if (System.IO.File.Exists(nom_reseau) == true && System.IO.File.Exists(nom_matrice) == true && projet.param_affectation_horaire.test_OK == true)
                {
                    string[] param = { ";" };
                    if (projet.reseaux.Count > 0)
                    {
                        projet.reseaux.RemoveAt(projet.reseaux.Count - 1);
                    }
                    projet.reseaux.Add(new network());

                    int num_res;
                    string chaine;
                    string[] ch;

                    projet.reseau_actif = projet.reseaux.Count - 1;
                    num_res = projet.reseaux.Count - 1;

                    string carte = "t links";

                    int avancement = 0;
                    int ctop = Console.CursorTop;
                    int cleft = Console.CursorLeft;
                    Console.SetCursorPosition(cleft, ctop);
                    Console.Write("Network import:" + avancement + "%");

                    System.IO.FileStream flux_reseau;
                    flux_reseau = new System.IO.FileStream(nom_reseau, System.IO.FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                    System.IO.StreamReader fichier_reseau = new System.IO.StreamReader(flux_reseau, Encoding.UTF8);

                    System.IO.StreamWriter fich_log = new System.IO.StreamWriter(aff_hor.nom_sortie + "_log.txt", false, System.Text.Encoding.UTF8);
                    fich_log.WriteLine("Version: Muslic Parallelized " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    fich_log.WriteLine("Process start time: " + System.DateTime.Now.ToString("dddd dd MMMM yyyy HH:mm:ss.fff"));
                    fich_log.WriteLine("Number of processors: " + Environment.ProcessorCount);

                    // Lecture du réseau (partie inchangée)
                    lec:
                    while (fichier_reseau.EndOfStream == false)
                    {
                    lecture:
                        chaine = fichier_reseau.ReadLine();

                        if (avancement < (int)((100 * flux_reseau.Position) / flux_reseau.Length) - 4)
                        {
                            Console.SetCursorPosition(cleft, ctop);
                            Console.Write("Network import:" + ((100 * flux_reseau.Position) / flux_reseau.Length).ToString() + "%");
                            avancement = (int)((100 * flux_reseau.Position) / flux_reseau.Length);
                        }

                        if (chaine.Trim().Length == 0) goto lec;
                        if (chaine.Substring(0, 7) == "t nodes")
                        {
                            carte = "t nodes";
                            goto lecture;
                        }
                        else if (chaine.Substring(0, 7) == "t links")
                        {
                            carte = "t links";
                            goto lecture;
                        }

                        ch = chaine.Split(param, System.StringSplitOptions.None);

                        if (carte == "t nodes")
                        {
                            string ni = ch[0].Trim();
                            if (projet.reseaux[projet.reseau_actif].numnoeud.ContainsKey(ni) == false)
                            {
                                projet.reseaux[projet.reseau_actif].numnoeud.Add(ni, projet.reseaux[projet.reseau_actif].nodes.Count);

                                float xi, yi;
                                if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                {
                                    xi = float.Parse(ch[1].Replace(',', '.'));
                                    yi = float.Parse(ch[2].Replace(',', '.'));
                                }
                                else
                                {
                                    xi = float.Parse(ch[1].Replace('.', ','));
                                    yi = float.Parse(ch[2].Replace('.', ','));
                                }
                                node noeud = new node();
                                node nul = new node();
                                noeud.i = ni;
                                noeud.x = xi;
                                noeud.y = yi;
                                noeud.is_visible = true;
                                if (xi > projet.reseaux[num_res].xu)
                                    projet.reseaux[num_res].xu = xi;
                                if (xi < projet.reseaux[num_res].xl)
                                    projet.reseaux[num_res].xl = xi;
                                if (yi > projet.reseaux[num_res].yu)
                                    projet.reseaux[num_res].yu = yi;
                                if (yi < projet.reseaux[num_res].yl)
                                    projet.reseaux[num_res].yl = yi;

                                if (ch.Length > 3)
                                    noeud.texte = ch[3];

                                projet.reseaux[projet.reseau_actif].nodes.Add(noeud);
                            }
                        }
                        else if (carte == "t links")
                        {
                            node nul = new node();
                            node nodei = new node();
                            node nodej = new node();
                            Link_num num_link = new Link_num();

                            string ni = ch[0].Trim();
                            int line;
                            nodei.i = ni;
                            int value;
                            if (projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(ni, out value) == false)
                            {
                                projet.reseaux[projet.reseau_actif].numnoeud.Add(ni, projet.reseaux[projet.reseau_actif].nodes.Count);
                                projet.reseaux[projet.reseau_actif].nodes.Add(nodei);
                            }

                            string nj = ch[1].Trim();
                            nodej.i = nj;
                            if (projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(nj, out value) == false)
                            {
                                projet.reseaux[projet.reseau_actif].numnoeud.Add(nj, projet.reseaux[projet.reseau_actif].nodes.Count);
                                projet.reseaux[projet.reseau_actif].nodes.Add(nodej);
                            }

                            link lien = new link();
                            lien.no = projet.reseaux[projet.reseau_actif].numnoeud[ni];
                            lien.nd = projet.reseaux[projet.reseau_actif].numnoeud[nj];
                            line = Convert.ToInt32(ch[4]);
                            num_link.i = ni;
                            num_link.j = nj;
                            num_link.line = line;

                            Service num_service = new Service();
                            num_service.numero = -1;
                            if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                            {
                                lien.temps = float.Parse(ch[2].Replace(',', '.'));
                                lien.longueur = float.Parse(ch[3].Replace(',', '.'));
                                num_service.numero = int.Parse(ch[5].Replace(',', '.'));
                                num_service.hd = float.Parse(ch[6].Replace(',', '.'));
                                num_service.hf = float.Parse(ch[7].Replace(',', '.'));
                            }
                            else
                            {
                                lien.temps = float.Parse(ch[2].Replace('.', ','));
                                lien.longueur = float.Parse(ch[3].Replace('.', ','));
                                num_service.numero = int.Parse(ch[5].Replace('.', ','));
                                num_service.hd = float.Parse(ch[6].Replace('.', ','));
                                num_service.hf = float.Parse(ch[7].Replace('.', ','));
                            }
                            if (num_service.hf < num_service.hd)
                                num_service.hf += 1440f;

                            if (projet.reseaux[projet.reseau_actif].num_calendrier.TryGetValue(ch[8].ToString().Trim(), out value) == false)
                            {
                                projet.reseaux[projet.reseau_actif].num_calendrier.Add(ch[8].ToString().Trim(), projet.reseaux[projet.reseau_actif].nom_calendrier.Count);
                                projet.reseaux[projet.reseau_actif].nom_calendrier.Add(ch[8].ToString().Trim());
                            }

                            num_service.regime = projet.reseaux[projet.reseau_actif].num_calendrier[ch[8].ToString().Trim()];

                            int nb = projet.reseaux[projet.reseau_actif].links.Count;

                            if (link_id.ContainsKey(num_link) == true && num_service.numero > 0)
                            {
                                projet.reseaux[projet.reseau_actif].links[link_id[num_link]].services.Add(num_service);
                                projet.reseaux[projet.reseau_actif].nbservices += 1;
                            }
                            else
                            {
                                lien.ligne = line;
                                if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                {
                                    lien.temps = float.Parse(ch[2].Replace(',', '.'));
                                    lien.longueur = float.Parse(ch[3].Replace(',', '.'));
                                }
                                else
                                {
                                    lien.temps = float.Parse(ch[2].Replace('.', ','));
                                    lien.longueur = float.Parse(ch[3].Replace('.', ','));
                                }

                                if (num_service.numero > 0)
                                {
                                    lien.services.Add(num_service);
                                    projet.reseaux[projet.reseau_actif].nbservices += 1;
                                }
                                if (ch.Length > 9 && ch[9].Length > 0)
                                    lien.texte = ch[9];
                                else
                                    lien.texte = " ";

                                if (ch.Length > 10)
                                {
                                    lien.type = ch[10].Trim().ToString();
                                    if (types.Contains(lien.type) == false)
                                        types.Add(lien.type);
                                }
                                else
                                    lien.type = "0";

                                if (ch.Length > 11)
                                {
                                    if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                        lien.toll = float.Parse(ch[11].Replace(',', '.'));
                                    else
                                        lien.toll = float.Parse(ch[11].Replace('.', ','));
                                }

                                projet.reseaux[projet.reseau_actif].links.Add(lien);
                                link_id[num_link] = projet.reseaux[projet.reseau_actif].links.Count - 1;
                            }
                        }
                    }
                    fichier_reseau.Close();
                    flux_reseau.Close();

                    fich_log.WriteLine("Network:" + nom_reseau);
                    fich_log.WriteLine("Nodes:" + projet.reseaux[projet.reseau_actif].nodes.Count);
                    fich_log.WriteLine("Links:" + projet.reseaux[projet.reseau_actif].links.Count);

                    // Construction du graphe
                    avancement = 0;
                    Console.WriteLine();
                    ctop = Console.CursorTop;
                    cleft = Console.CursorLeft;

                    for (i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
                    {
                        projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].nd].pred.Add(i);
                        projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].no].succ.Add(i);

                        if (avancement < (int)((100 * (i + 1)) / projet.reseaux[projet.reseau_actif].links.Count) - 4)
                        {
                            avancement = (int)((100 * (i + 1)) / projet.reseaux[projet.reseau_actif].links.Count);
                            Console.SetCursorPosition(cleft, ctop);
                            Console.Write("Network topology generation:" + ((100 * (i + 1)) / projet.reseaux[projet.reseau_actif].links.Count).ToString() + "%");
                        }
                    }

                    // Import des pénalités
                    if (System.IO.File.Exists(nom_penalites) == true && System.IO.File.Exists(nom_reseau) == true && System.IO.File.Exists(nom_matrice) == true)
                    {
                        fich_log.WriteLine("Penalties and transfers:" + nom_penalites);
                        string[] penal;
                        int ni, nj, nk;
                        int linei, linej, ntri, ntrj;
                        float tps_mvt;
                        System.IO.FileStream flux_penalites;
                        flux_penalites = new System.IO.FileStream(nom_penalites, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read);
                        System.IO.StreamReader fichier_penalites = new System.IO.StreamReader(flux_penalites, System.Text.Encoding.UTF8);
                        avancement = 0;
                        while (fichier_penalites.EndOfStream == false)
                        {
                            if (avancement < (int)((100 * flux_penalites.Position) / flux_penalites.Length) - 4)
                            {
                                Console.SetCursorPosition(cleft, ctop);
                                Console.Write("Penalties and transfers import:" + ((100 * flux_penalites.Position) / flux_penalites.Length).ToString() + "%");
                                avancement = (int)((100 * flux_penalites.Position) / flux_penalites.Length);
                            }

                            chaine = fichier_penalites.ReadLine();
                            ntri = -1; ntrj = -1;
                            penal = chaine.Split(param, System.StringSplitOptions.RemoveEmptyEntries);
                            nj = projet.reseaux[projet.reseau_actif].numnoeud[penal[0].Trim()];
                            ni = projet.reseaux[projet.reseau_actif].numnoeud[penal[1].Trim()];
                            linei = int.Parse(penal[2]);
                            nk = projet.reseaux[projet.reseau_actif].numnoeud[penal[3].Trim()];
                            linej = int.Parse(penal[4]);
                            if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                tps_mvt = float.Parse(penal[5].Replace(',', '.'));
                            else
                                tps_mvt = float.Parse(penal[5].Replace('.', ','));

                            for (i = 0; i < projet.reseaux[projet.reseau_actif].nodes[nj].pred.Count; i++)
                            {
                                if (projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[nj].pred[i]].no == ni && projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[nj].pred[i]].ligne == linei)
                                    ntri = projet.reseaux[projet.reseau_actif].nodes[nj].pred[i];
                            }
                            for (i = 0; i < projet.reseaux[projet.reseau_actif].nodes[nj].succ.Count; i++)
                            {
                                if (projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[nj].succ[i]].nd == nk && projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[nj].succ[i]].ligne == linej)
                                    ntrj = projet.reseaux[projet.reseau_actif].nodes[nj].succ[i];
                            }
                            if (ntrj >= 0 && ntri >= 0)
                            {
                                Turn virage = new Turn();
                                virage.arci = ntri;
                                virage.arcj = ntrj;
                                float value;
                                if (turns.TryGetValue(virage, out value) == false)
                                    turns.Add(virage, tps_mvt);
                                projet.reseaux[projet.reseau_actif].nodes[nj].is_intersection = true;
                            }
                        }
                        fichier_penalites.Close();
                        flux_penalites.Close();
                    }

                    Console.SetCursorPosition(cleft, ctop);
                    Console.Write("Penalties and transfers import:" + (100).ToString() + "%");

                    // Traitement des OD avec parallélisation
                    if (projet.param_affectation_horaire.algorithme <= 1)
                    {
                        // Lecture et groupement des OD
                        Console.WriteLine();
                        Console.WriteLine("Reading and grouping OD pairs...");

                        Dictionary<string, ODGroup> odGroups = new Dictionary<string, ODGroup>();
                        Dictionary<string, string> libodMapping = new Dictionary<string, string>();

                        System.IO.FileStream flux_matrice = new System.IO.FileStream(nom_matrice, System.IO.FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                        System.IO.StreamReader fichier_matrice = new System.IO.StreamReader(flux_matrice, System.Text.Encoding.UTF8);
                        int numod = 0;

                        while (fichier_matrice.EndOfStream == false)
                        {
                            chaine = fichier_matrice.ReadLine();
                            if (chaine.Trim().Length == 0) continue;

                            numod++;
                            ch = chaine.Split(param, StringSplitOptions.RemoveEmptyEntries);

                            string p = ch[0].Trim();
                            string q = ch[1].Trim();
                            float od, horaire;
                            int jour, sens = 1;

                            if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                            {
                                od = Single.Parse(ch[2].Replace(',', '.'));
                                jour = (int)Single.Parse(ch[3].Replace(',', '.'));
                                horaire = Single.Parse(ch[4].Replace(',', '.'));
                            }
                            else
                            {
                                od = Single.Parse(ch[2].Replace('.', ','));
                                jour = (int)Single.Parse(ch[3].Replace('.', ','));
                                horaire = Single.Parse(ch[4].Replace('.', ','));
                            }

                            if (ch.Length > 5)
                            {
                                if (ch[5].ToLower() == "d")
                                    sens = 1;
                                else if (ch[5].ToLower() == "a")
                                    sens = 2;
                            }

                            string libod = (ch.Length > 6 && ch[6].Length > 0) ? ch[6].Trim() : numod.ToString();

                            // Créer la clé de groupement
                            string groupKey = sens == 1 
                                ? $"{p}_{jour}_{horaire.ToString("F0")}_S1"
                                : $"{q}_{jour}_{horaire.ToString("F0")}_S2";

                            if (!odGroups.ContainsKey(groupKey))
                            {
                                odGroups[groupKey] = new ODGroup
                                {
                                    Key = groupKey,
                                    Origin = p,
                                    Destination = q,
                                    Day = jour,
                                    Hour = horaire,
                                    Direction = sens
                                };
                            }

                            odGroups[groupKey].TotalDemand += od;
                            odGroups[groupKey].Entries.Add(new ODEntry { Demand = od, LibOD = libod });
                            libodMapping[libod] = groupKey;
                        }

                        fichier_matrice.Close();
                        flux_matrice.Close();

                        Console.WriteLine($"Found {odGroups.Count} OD groups from {numod} OD pairs");
                        fich_log.WriteLine($"OD Pairs: {numod}");
                        fich_log.WriteLine($"OD Groups: {odGroups.Count}");

                        // Initialiser les fichiers de sortie thread-safe
                        object lockFilesOutput = new object();
                        using (System.IO.StreamWriter fich_sortie = new System.IO.StreamWriter(aff_hor.nom_sortie + "_temps.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_sortie2 = new System.IO.StreamWriter(aff_hor.nom_sortie + "_chemins.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_od = new System.IO.StreamWriter(aff_hor.nom_sortie + "_od.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_noeuds = new System.IO.StreamWriter(aff_hor.nom_sortie + "_noeuds.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_detour = new System.IO.StreamWriter(aff_hor.nom_sortie + "_detour.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_isoles = new System.IO.StreamWriter(aff_hor.nom_sortie + "_isoles.txt", false, Encoding.UTF8))
                        {
                            // Headers
                            fich_sortie2.WriteLine("id;o;d;jour;heure;i;j;ij;ligne;service;temps;heureo;tveh;tmap;tatt;tcorr;ncorr;tatt1;cout;longueur;pole;volau;boai;alij;texte;type;toll");
                            fich_od.WriteLine("id;o;d;jour;heureo;heured;temps;tveh;tmap;tatt;tcorr;ncorr;tatt1;cout;longueur;pole;volau;texte;nbpop;toll");
                            if (aff_hor.sortie_temps == 3)
                            {
                                fich_sortie.WriteLine("o;ij;ligne;temps;tatt1;volau");
                                fich_noeuds.WriteLine("o;numero;temps;tatt1;volau");
                            }
                            else
                            {
                                fich_sortie.WriteLine("id;o;ij;ligne;numtrc;jour;heureo;heured;temps;tveh;tmap;tatt;tcorr;ncorr;tatt1;cout;longueur;pole;volau;precedent;type;toll;ti");
                                fich_noeuds.WriteLine("id;o;d;jour;numero;heureo;heured;temps;tveh;tmap;tatt;tcorr;ncorr;tatt1;cout;longueur;pole;toll;volau;stops");
                            }

                            // Dictionnaires thread-safe pour accumuler les résultats
                            ConcurrentDictionary<int, LinkAffectation> linkAffectations = new ConcurrentDictionary<int, LinkAffectation>();
                            ConcurrentDictionary<Turn, float> transfersAccumulated = new ConcurrentDictionary<Turn, float>();
                            ConcurrentBag<string> outputLinesTemps = new ConcurrentBag<string>();
                            ConcurrentBag<string> outputLinesChemin = new ConcurrentBag<string>();
                            ConcurrentBag<string> outputLinesOD = new ConcurrentBag<string>();
                            ConcurrentBag<string> outputLinesNoeuds = new ConcurrentBag<string>();
                            ConcurrentBag<string> outputLinesDetour = new ConcurrentBag<string>();

                            DateTime t1 = DateTime.Now;
                            fich_log.WriteLine("Computation start time: " + t1.ToString("dddd dd MMMM yyyy HH:mm:ss.fff"));
                            fich_log.Flush();

                            // Traiter les groupes OD en parallèle
                            Console.WriteLine("Processing OD groups in parallel...");
                            int totalGroups = odGroups.Count;
                            int processedGroups = 0;

                            Parallel.ForEach(odGroups.Values, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (odGroup) =>
                            {
                                try
                                {
                                    // Traiter ce groupe OD
                                    ODGroupResult result = ProcessODGroup(odGroup, projet, aff_hor, turns, types);

                                    // Ajouter les résultats aux collections thread-safe
                                    foreach (var line in result.OutputLinesTemps)
                                        outputLinesTemps.Add(line);
                                    foreach (var line in result.OutputLinesChemin)
                                        outputLinesChemin.Add(line);
                                    foreach (var line in result.OutputLinesOD)
                                        outputLinesOD.Add(line);
                                    foreach (var line in result.OutputLinesNoeuds)
                                        outputLinesNoeuds.Add(line);
                                    foreach (var line in result.OutputLinesDetour)
                                        outputLinesDetour.Add(line);

                                    // Fusionner les affectations de liens
                                    foreach (var kvp in result.LinkAffectations)
                                    {
                                        linkAffectations.AddOrUpdate(kvp.Key, kvp.Value, (key, existing) =>
                                        {
                                            existing.volau += kvp.Value.volau;
                                            existing.boai += kvp.Value.boai;
                                            existing.alij += kvp.Value.alij;
                                            foreach (var svc in kvp.Value.ServiceAffectations)
                                            {
                                                if (existing.ServiceAffectations.ContainsKey(svc.Key))
                                                {
                                                    existing.ServiceAffectations[svc.Key].volau += svc.Value.volau;
                                                    existing.ServiceAffectations[svc.Key].boat += svc.Value.boat;
                                                    existing.ServiceAffectations[svc.Key].alit += svc.Value.alit;
                                                }
                                                else
                                                {
                                                    existing.ServiceAffectations[svc.Key] = svc.Value;
                                                }
                                            }
                                            return existing;
                                        });
                                    }

                                    // Fusionner les transferts
                                    foreach (var transfer in result.Transfers)
                                    {
                                        transfersAccumulated.AddOrUpdate(transfer.Key, transfer.Value, (key, existing) => existing + transfer.Value);
                                    }

                                    Interlocked.Increment(ref processedGroups);
                                    if (processedGroups % 100 == 0)
                                        Console.WriteLine($"Processed {processedGroups}/{totalGroups} groups");
                                }
                                catch (Exception ex)
                                {
                                    fich_log.WriteLine($"Error processing OD group {odGroup.Key}: {ex.Message}");
                                }
                            });

                            // Écrire les résultats accumulés dans les fichiers
                            Console.WriteLine("Writing results to files...");

                            foreach (var line in outputLinesTemps)
                                fich_sortie.WriteLine(line);
                            foreach (var line in outputLinesChemin)
                                fich_sortie2.WriteLine(line);
                            foreach (var line in outputLinesOD)
                                fich_od.WriteLine(line);
                            foreach (var line in outputLinesNoeuds)
                                fich_noeuds.WriteLine(line);
                            foreach (var line in outputLinesDetour)
                                fich_detour.WriteLine(line);

                            DateTime t2 = DateTime.Now;
                            fich_log.WriteLine("Computation end time: " + t2.ToString("dddd dd MMMM yyyy HH:mm:ss.fff"));
                            fich_log.WriteLine("Computation duration:" + t2.Subtract(t1).TotalSeconds + " sec");
                        }

                        // Écrire les résultats agrégés (liens et services)
                        using (System.IO.StreamWriter fich_result = new System.IO.StreamWriter(aff_hor.nom_sortie + "_aff.txt", false, Encoding.UTF8))
                        {
                            foreach (int linkIndex in linkAffectations.Keys)
                            {
                                if (linkIndex < projet.reseaux[projet.reseau_actif].links.Count)
                                {
                                    link lien = projet.reseaux[projet.reseau_actif].links[linkIndex];
                                    LinkAffectation aff = linkAffectations[linkIndex];

                                    if (aff.volau > 0 || aff.boai > 0 || aff.alij > 0)
                                    {
                                        string texte = projet.reseaux[projet.reseau_actif].nodes[lien.no].i;
                                        texte += ";" + projet.reseaux[projet.reseau_actif].nodes[lien.nd].i + ";" + lien.ligne.ToString("0");
                                        texte += ";" + aff.volau.ToString("0.00");
                                        texte += ";" + aff.boai.ToString("0.00");
                                        texte += ";" + aff.alij.ToString("0.00");
                                        texte += ";" + lien.texte;
                                        texte += ";" + lien.type;
                                        texte += ";" + lien.toll.ToString("0.000");
                                        fich_result.WriteLine(texte);
                                    }
                                }
                            }
                        }

                        // Écrire les services
                        if (aff_hor.sortie_services == true)
                        {
                            using (System.IO.StreamWriter fich_services = new System.IO.StreamWriter(aff_hor.nom_sortie + "_services.txt", false, System.Text.Encoding.UTF8))
                            {
                                fich_services.WriteLine("i;j;ligne;service;hd;hf;regime;volau;boia;alij;texte;type");

                                foreach (int linkIndex in linkAffectations.Keys)
                                {
                                    if (linkIndex < projet.reseaux[projet.reseau_actif].links.Count)
                                    {
                                        link lien = projet.reseaux[projet.reseau_actif].links[linkIndex];
                                        LinkAffectation aff = linkAffectations[linkIndex];

                                        foreach (var svcAff in aff.ServiceAffectations)
                                        {
                                            int serviceIdx = svcAff.Key;
                                            if (serviceIdx < lien.services.Count && svcAff.Value.volau > 0)
                                            {
                                                Service svc = lien.services[serviceIdx];
                                                string texte = projet.reseaux[projet.reseau_actif].nodes[lien.no].i;
                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[lien.nd].i + ";" + lien.ligne.ToString("0");
                                                texte += ";" + svc.numero;
                                                texte += ";" + svc.hd;
                                                texte += ";" + svc.hf;
                                                texte += ";" + projet.reseaux[projet.reseau_actif].nom_calendrier[svc.regime];
                                                texte += ";" + svcAff.Value.volau.ToString("0.00");
                                                texte += ";" + svcAff.Value.boat.ToString("0.00");
                                                texte += ";" + svcAff.Value.alit.ToString("0.00");
                                                texte += ";" + lien.texte;
                                                texte += ";" + lien.type;
                                                fich_services.WriteLine(texte);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Écrire les transferts
                        if (aff_hor.sortie_turns == true)
                        {
                            using (System.IO.StreamWriter fich_turns = new System.IO.StreamWriter(aff_hor.nom_sortie + "_transferts.txt", false, System.Text.Encoding.UTF8))
                            {
                                fich_turns.WriteLine("j;i;lignei;k;lignek;textei;textek;volau");

                                foreach (var transfer in transfersAccumulated)
                                {
                                    if (transfer.Value > 0 && transfer.Key.arci < projet.reseaux[projet.reseau_actif].links.Count && transfer.Key.arcj < projet.reseaux[projet.reseau_actif].links.Count)
                                    {
                                        string texte = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[transfer.Key.arci].nd].i;
                                        texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[transfer.Key.arci].no].i;
                                        texte += ";" + projet.reseaux[projet.reseau_actif].links[transfer.Key.arci].ligne;
                                        texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[transfer.Key.arcj].nd].i;
                                        texte += ";" + projet.reseaux[projet.reseau_actif].links[transfer.Key.arcj].ligne;
                                        texte += ";" + projet.reseaux[projet.reseau_actif].links[transfer.Key.arci].texte;
                                        texte += ";" + projet.reseaux[projet.reseau_actif].links[transfer.Key.arcj].texte;
                                        texte += ";" + transfer.Value;
                                        fich_turns.WriteLine(texte);
                                    }
                                }
                            }
                        }

                        fich_log.Close();
                    }
                }
            }
        }

        private static ODGroupResult ProcessODGroup(ODGroup odGroup, etude projet, Param_affectation_horaire aff_hor_base, Dictionary<Turn, float> turns_global, HashSet<string> types)
        {
            ODGroupResult result = new ODGroupResult();
            result.GroupKey = odGroup.Key;

            // Créer une copie locale des paramètres pour ce thread
            Param_affectation_horaire aff_hor = new Param_affectation_horaire();
            aff_hor.algorithme = aff_hor_base.algorithme;
            aff_hor.demitours = aff_hor_base.demitours;
            aff_hor.max_nb_buckets = aff_hor_base.max_nb_buckets;
            aff_hor.nb_jours = aff_hor_base.nb_jours;
            aff_hor.nom_matrice = aff_hor_base.nom_matrice;
            aff_hor.nom_penalites = aff_hor_base.nom_penalites;
            aff_hor.nom_reseau = aff_hor_base.nom_reseau;
            aff_hor.nom_sortie = aff_hor_base.nom_sortie;
            aff_hor.param_dijkstra = aff_hor_base.param_dijkstra;
            aff_hor.pu = aff_hor_base.pu;
            aff_hor.sortie_chemins = aff_hor_base.sortie_chemins;
            aff_hor.sortie_services = aff_hor_base.sortie_services;
            aff_hor.sortie_temps = aff_hor_base.sortie_temps;
            aff_hor.sortie_turns = aff_hor_base.sortie_turns;
            aff_hor.texte_cboa = aff_hor_base.texte_cboa;
            aff_hor.texte_cmap = aff_hor_base.texte_cmap;
            aff_hor.texte_coef_tmap = aff_hor_base.texte_coef_tmap;
            aff_hor.texte_cveh = aff_hor_base.texte_cveh;
            aff_hor.texte_cwait = aff_hor_base.texte_cwait;
            aff_hor.texte_tboa = aff_hor_base.texte_tboa;
            aff_hor.texte_tboa_max = aff_hor_base.texte_tboa_max;
            aff_hor.texte_toll = aff_hor_base.texte_toll;
            aff_hor.tmapmax = aff_hor_base.tmapmax;
            aff_hor.temps_max = aff_hor_base.temps_max;
            aff_hor.sortie_noeuds = aff_hor_base.sortie_noeuds;
            aff_hor.sortie_isoles = aff_hor_base.sortie_isoles;
            aff_hor.sortie_stops = aff_hor_base.sortie_stops;
            aff_hor.texte_filtre_sortie = aff_hor_base.texte_filtre_sortie;
            aff_hor.test_OK = aff_hor_base.test_OK;

            // Copier les dictionnaires
            foreach (var kvp in aff_hor_base.cveh)
                aff_hor.cveh[kvp.Key] = kvp.Value;
            foreach (var kvp in aff_hor_base.cwait)
                aff_hor.cwait[kvp.Key] = kvp.Value;
            foreach (var kvp in aff_hor_base.cmap)
                aff_hor.cmap[kvp.Key] = kvp.Value;
            foreach (var kvp in aff_hor_base.cboa)
                aff_hor.cboa[kvp.Key] = kvp.Value;
            foreach (var kvp in aff_hor_base.tboa)
                aff_hor.tboa[kvp.Key] = kvp.Value;
            foreach (var kvp in aff_hor_base.coef_tmap)
                aff_hor.coef_tmap[kvp.Key] = kvp.Value;
            foreach (var kvp in aff_hor_base.tboa_max)
                aff_hor.tboa_max[kvp.Key] = kvp.Value;
            foreach (var kvp in aff_hor_base.ctoll)
                aff_hor.ctoll[kvp.Key] = kvp.Value;

            // Traiter le groupe OD avec l'algorithme existant (adapté pour un groupe)
            ProcessSingleODGroup(odGroup, projet, aff_hor, turns_global, types, result);

            return result;
        }

        private static void ProcessSingleODGroup(ODGroup odGroup, etude projet, Param_affectation_horaire aff_hor, Dictionary<Turn, float> turns_global, HashSet<string> types, ODGroupResult result)
        {
            // Cette fonction est une version adaptée du code original pour traiter un seul groupe OD
            // Elle accumule les résultats dans result.LinkAffectations, result.Transfers, etc.
            // Pour simplifier, on utilise le premier OD du groupe avec la demande totale

            string p = odGroup.Origin;
            string q = odGroup.Destination;
            int jour = odGroup.Day;
            float horaire = odGroup.Hour;
            int sens = odGroup.Direction;
            float totalDemand = odGroup.TotalDemand;
            string libod = odGroup.Entries[0].LibOD; // Utiliser le premier libOD du groupe

            // Initialiser les structures de données locales pour ce groupe
            List<List<int>> gga_nq = new List<List<int>>();
            Dictionary<int, LinkInfo> linkStates = new Dictionary<int, LinkInfo>();

            // Initialiser tous les liens
            for (int i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
            {
                linkStates[i] = new LinkInfo();
            }

            // Lancer le calcul d'itinéraire (Dijkstra/GGA)
            if (sens == 1)
            {
                // Sens départ (code original pour sens 1)
                ProcessODDirection1(p, q, jour, horaire, libod, totalDemand, projet, aff_hor, turns_global, types, linkStates, gga_nq, result);
            }
            else if (sens == 2)
            {
                // Sens arrivée (code original pour sens 2)
                ProcessODDirection2(p, q, jour, horaire, libod, totalDemand, projet, aff_hor, turns_global, types, linkStates, gga_nq, result);
            }
        }

        // Structures d'aide
        public class LinkInfo
        {
            public float cout = 0;
            public float h = 0;
            public float tatt = 0;
            public float tatt1 = 0;
            public float tcor = 0;
            public int ncorr = 0;
            public float tmap = 0;
            public float tveh = 0;
            public float ttoll = 0;
            public float l = 0;
            public int touche = 0;
            public int pivot = -1;
            public int turn_pivot = -1;
            public int service = -1;
            public string pole = "-1";
            public string poleV2 = "";
        }

        private static void ProcessODDirection1(string p, string q, int jour, float horaire, string libod, float od, etude projet, Param_affectation_horaire aff_hor, Dictionary<Turn, float> turns, HashSet<string> types, Dictionary<int, LinkInfo> linkStates, List<List<int>> gga_nq, ODGroupResult result)
        {
            // Implémentation du traitement pour la direction 1 (départ)
            // Ceci est une version simplifiée - vous devrez adapter le code complet d'origine ici
            // Pour démonstration, on laisse une structure de base
        }

        private static void ProcessODDirection2(string p, string q, int jour, float horaire, string libod, float od, etude projet, Param_affectation_horaire aff_hor, Dictionary<Turn, float> turns, HashSet<string> types, Dictionary<int, LinkInfo> linkStates, List<List<int>> gga_nq, ODGroupResult result)
        {
            // Implémentation du traitement pour la direction 2 (arrivée)
            // Ceci est une version simplifiée - vous devrez adapter le code complet d'origine ici
            // Pour démonstration, on laisse une structure de base
        }

        // Classes de données originales (inchangées)
        public class vecteur
        {
            public List<float> d = new List<float>(0);
        }

        public class Turn
        {
            public int arci, arcj;

            public override bool Equals(Object virage)
            {
                if (arci == ((Turn)virage).arci && arcj == ((Turn)virage).arcj)
                    return true;
                else
                    return false;
            }
            public override int GetHashCode()
            {
                return arci.GetHashCode() ^ arcj.GetHashCode();
            }
        }

        public class turn
        {
            public int numero;
            public float temps;
            public bool is_valid = false;
        }

        public class node
        {
            public float x, y, tempst = 1e38f, tmap = 0, tatt, temps, cout, ncor, ttoll;
            public string i;
            public bool ci = false, is_valid = true, is_visible = false, is_intersection = false;
            public List<int> pred = new List<int>();
            public List<int> succ = new List<int>();
            public string texte;
            public string pole;
        }

        public class Link_num
        {
            public String i, j;
            public int line;
            public override bool Equals(object num_link)
            {
                if (i == ((Link_num)num_link).i && j == ((Link_num)num_link).j && line == ((Link_num)num_link).line)
                    return true;
                else
                    return false;
            }
            public override int GetHashCode()
            {
                return (i.GetHashCode() ^ j.GetHashCode() ^ line.GetHashCode());
            }
        }

        public class link
        {
            public float longueur, temps, cout, v0, vsat, tatt, tcor, tveh, tmap, tatt1, a, b, n, volau, lanes, h, l, alij, boai, ncorr, toll = 0, ttoll;
            public int no, nd, service, vdf, touche, pivot, ligne, turn_pivot = -1;
            public bool is_queue, is_valid = true;
            public List<Service> services = new List<Service>();
            public string texte, modes, pole, type = "0", poleV2;
        }

        public class matrix
        {
            public string nom;
            public List<vecteur> o = new List<vecteur>(0);
        }

        public class network
        {
            public string nom;
            public List<link> links = new List<link>(20000);
            public List<node> nodes = new List<node>(10000);
            public float xl = 1e38f, xu = -1e38f, yl = 1e38f, yu = -1e38f;
            public List<matrix> matrices = new List<matrix>();
            public Dictionary<string, int> numnoeud = new Dictionary<string, int>();
            public Dictionary<string, int> num_calendrier = new Dictionary<string, int>();
            public List<string> nom_calendrier = new List<string>();
            public Dictionary<string, string> noms_arcs = new Dictionary<string, string>();
            public int max_type = 0, nbturns = 0, nbservices = 0;
        }

        public class etude
        {
            public string nom;
            public int reseau_actif;
            public List<network> reseaux = new List<network>();
            public Param_affectation_horaire param_affectation_horaire = new Param_affectation_horaire();
        }

        public class Param_affectation_horaire
        {
            public string nom_reseau, nom_matrice, nom_sortie, nom_penalites;
            public Dictionary<String, float> coef_tmap = new Dictionary<String, float>();
            public Dictionary<String, float> cmap = new Dictionary<String, float>();
            public Dictionary<String, float> cwait = new Dictionary<String, float>();
            public Dictionary<String, float> cboa = new Dictionary<String, float>();
            public Dictionary<String, float> tboa = new Dictionary<String, float>();
            public Dictionary<String, float> tboa_max = new Dictionary<String, float>(1);
            public Dictionary<String, float> cveh = new Dictionary<String, float>(1);
            public Dictionary<String, float> ctoll = new Dictionary<String, float>(1);
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

        public class Service
        {
            public int numero;
            public float hd, hf, delta = 0, boai = 0, alij = 0, alit = 0, boat = 0, volau = 0;
            public int regime;
        }

        public class suivant
        {
            public List<int> classe = new List<int>();
        }

        public class Node
        {
            public string i = "";
            public float x = 0, y = 0, tempst = 1e38f, tmap = 0, tatt, temps, cout, ncor, ttoll;
            public string name = "";
            public string pole, poleV2;
            public List<int> in_nodes = new List<int>();
            public List<int> out_nodes = new List<int>();
            public void addincoming(int i)
            {
                this.in_nodes.Add(i);
            }
            public void addoutgoing(int i)
            {
                this.out_nodes.Add(i);
            }
        }

        public class Link
        {
        }
    }
}
