using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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

        // Structures pour grouper les OD
        public class ODGroup
        {
            public string Key { get; set; }
            public string Origin { get; set; }
            public string Destination { get; set; }
            public int Day { get; set; }
            public float Hour { get; set; }
            public int Direction { get; set; }
            public float TotalDemand { get; set; }
            public List<ODEntry> Entries { get; set; } = new List<ODEntry>();
        }

        public class ODEntry
        {
            public float Demand { get; set; }
            public string LibOD { get; set; }
        }

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
            public List<Service> services = new List<Service>();

        }

        public static void affectation_tc(string nom_reseau, string nom_matrice, string nom_sortie, string nom_parametres, string nom_penalites)
        {
            int i, j;

            HashSet<String> types = new HashSet<string>();
            Dictionary<Turn, float> turns = new Dictionary<Turn, float>();
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

                    // Lecture du réseau
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
                    if (System.IO.File.Exists(nom_penalites) == true)
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
                        Console.WriteLine();
                        Console.WriteLine("Reading and grouping OD pairs...");

                        Dictionary<string, ODGroup> odGroups = new Dictionary<string, ODGroup>();

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
                        }

                        fichier_matrice.Close();
                        flux_matrice.Close();

                        Console.WriteLine($"Found {odGroups.Count} OD groups from {numod} OD pairs");
                        fich_log.WriteLine($"OD Pairs: {numod}");
                        fich_log.WriteLine($"OD Groups: {odGroups.Count}");

                        using (System.IO.StreamWriter fich_sortie = new System.IO.StreamWriter(aff_hor.nom_sortie + "_temps.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_sortie2 = new System.IO.StreamWriter(aff_hor.nom_sortie + "_chemins.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_od = new System.IO.StreamWriter(aff_hor.nom_sortie + "_od.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_noeuds = new System.IO.StreamWriter(aff_hor.nom_sortie + "_noeuds.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_detour = new System.IO.StreamWriter(aff_hor.nom_sortie + "_detour.txt", false, Encoding.UTF8))
                        using (System.IO.StreamWriter fich_isoles = new System.IO.StreamWriter(aff_hor.nom_sortie + "_isoles.txt", false, Encoding.UTF8))
                        {
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

                            ConcurrentDictionary<int, LinkAffectation> linkAffectations = new ConcurrentDictionary<int, LinkAffectation>();
                            ConcurrentDictionary<Turn, float> transfersAccumulated = new ConcurrentDictionary<Turn, float>();
                            ConcurrentBag<string> outputLinesTemps = new ConcurrentBag<string>();
                            ConcurrentBag<string> outputLinesChemin = new ConcurrentBag<string>();
                            ConcurrentBag<string> outputLinesOD = new ConcurrentBag<string>();
                            ConcurrentBag<string> outputLinesNoeuds = new ConcurrentBag<string>();
                            ConcurrentBag<string> outputLinesDetour = new ConcurrentBag<string>();
                            ConcurrentBag<string> outputLog = new ConcurrentBag<string>();

                            DateTime t1 = DateTime.Now;
                            fich_log.WriteLine("Computation start time: " + t1.ToString("dddd dd MMMM yyyy HH:mm:ss.fff"));
                            fich_log.Flush();

                            Console.WriteLine("Processing OD groups in parallel...");
                            int totalGroups = odGroups.Count;
                            int processedGroups = 0;

                            Parallel.ForEach(odGroups.Values, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (odGroup) =>
                            {
                                try
                                {
                                    ODGroupResult result = ProcessODGroup(odGroup, projet, aff_hor, turns, types);

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

                                    foreach (var kvp in result.LinkAffectations)
                                    {
                                        linkAffectations.AddOrUpdate(kvp.Key, kvp.Value, (key, existing) =>
                                        {
                                            lock (existing)
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
                                            }
                                            return existing;
                                        });
                                    }

                                    foreach (var transfer in result.Transfers)
                                    {
                                        transfersAccumulated.AddOrUpdate(transfer.Key, transfer.Value, (key, existing) => existing + transfer.Value);
                                    }

                                    Interlocked.Increment(ref processedGroups);
                                    if (processedGroups % Math.Max(1, totalGroups / 10) == 0)
                                        Console.WriteLine($"Processed {processedGroups}/{totalGroups} groups");
                                }
                                catch (Exception ex)
                                {
                                    lock (fich_log)
                                    {
                                        fich_log.WriteLine($"Error processing OD group {odGroup.Key}: {ex.Message}");
                                    }
                                }
                            });

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

            Param_affectation_horaire aff_hor = CloneAffectationParams(aff_hor_base);

            string p = odGroup.Origin;
            string q = odGroup.Destination;
            int jour = odGroup.Day;
            float horaire = odGroup.Hour;
            int sens = odGroup.Direction;
            float totalDemand = odGroup.TotalDemand;
            string libod = odGroup.Entries[0].LibOD;

            List<List<int>> gga_nq = new List<List<int>>();
            Dictionary<int, LinkInfo> linkStates = new Dictionary<int, LinkInfo>();

            for (int i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
            {
                linkStates[i] = new LinkInfo();
            }

            if (sens == 1)
            {
                ProcessODDirection1(p, q, jour, horaire, libod, totalDemand, projet, aff_hor, turns_global, types, linkStates, gga_nq, result);
            }
            else if (sens == 2)
            {
                ProcessODDirection2(p, q, jour, horaire, libod, totalDemand, projet, aff_hor, turns_global, types, linkStates, gga_nq, result);
            }

            return result;
        }

        private static Param_affectation_horaire CloneAffectationParams(Param_affectation_horaire original)
        {
            Param_affectation_horaire clone = new Param_affectation_horaire();
            clone.algorithme = original.algorithme;
            clone.demitours = original.demitours;
            clone.max_nb_buckets = original.max_nb_buckets;
            clone.nb_jours = original.nb_jours;
            clone.nom_matrice = original.nom_matrice;
            clone.nom_penalites = original.nom_penalites;
            clone.nom_reseau = original.nom_reseau;
            clone.nom_sortie = original.nom_sortie;
            clone.param_dijkstra = original.param_dijkstra;
            clone.pu = original.pu;
            clone.sortie_chemins = original.sortie_chemins;
            clone.sortie_services = original.sortie_services;
            clone.sortie_temps = original.sortie_temps;
            clone.sortie_turns = original.sortie_turns;
            clone.texte_cboa = original.texte_cboa;
            clone.texte_cmap = original.texte_cmap;
            clone.texte_coef_tmap = original.texte_coef_tmap;
            clone.texte_cveh = original.texte_cveh;
            clone.texte_cwait = original.texte_cwait;
            clone.texte_tboa = original.texte_tboa;
            clone.texte_tboa_max = original.texte_tboa_max;
            clone.texte_toll = original.texte_toll;
            clone.tmapmax = original.tmapmax;
            clone.temps_max = original.temps_max;
            clone.sortie_noeuds = original.sortie_noeuds;
            clone.sortie_isoles = original.sortie_isoles;
            clone.sortie_stops = original.sortie_stops;
            clone.texte_filtre_sortie = original.texte_filtre_sortie;
            clone.test_OK = original.test_OK;
            clone.nb_pop = 0;

            foreach (var kvp in original.cveh)
                clone.cveh[kvp.Key] = kvp.Value;
            foreach (var kvp in original.cwait)
                clone.cwait[kvp.Key] = kvp.Value;
            foreach (var kvp in original.cmap)
                clone.cmap[kvp.Key] = kvp.Value;
            foreach (var kvp in original.cboa)
                clone.cboa[kvp.Key] = kvp.Value;
            foreach (var kvp in original.tboa)
                clone.tboa[kvp.Key] = kvp.Value;
            foreach (var kvp in original.coef_tmap)
                clone.coef_tmap[kvp.Key] = kvp.Value;
            foreach (var kvp in original.tboa_max)
                clone.tboa_max[kvp.Key] = kvp.Value;
            foreach (var kvp in original.ctoll)
                clone.ctoll[kvp.Key] = kvp.Value;

            return clone;
        }

        private static void ProcessODDirection1(string p, string q, int jour, float horaire, string libod, float od, etude projet, Param_affectation_horaire aff_hor, Dictionary<Turn, float> turns_global, HashSet<string> types, Dictionary<int, LinkInfo> linkStates, List<List<int>> gga_nq, ODGroupResult result)
        {
            int i, j;
            string[] param = { ";" };

            // Initialiser les liens

            var linkStates= linkStates
                .Select(l => l.Clone())
                .ToList();

            gga_nq.Clear();
            string depart = p;
            int pivot = -1, value;
            int successeur, bucket, id_bucket = 0;
            String succ_type;
            float penalite = 0, temps_correspondance, max_correspondance;

            HashSet<String> filtre = new HashSet<String>();
            if (aff_hor.texte_filtre_sortie.Trim().Length > 0)
            {
                string[] ch_filtre = aff_hor.texte_filtre_sortie.Split('|');
                for (int f = 0; f < ch_filtre.Length; f++)
                {
                    if (filtre.Contains(ch_filtre[f].Trim()) == false)
                        filtre.Add(ch_filtre[f].Trim());
                }
            }

            if (projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(p, out value) == true)
            {
                for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[depart]].succ.Count; j++)
                {
                    successeur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[depart]].succ[j];
                    succ_type = linkStates[successeur].type;
                    max_correspondance = aff_hor.tboa_max[succ_type];

                    // Marche à pied
                    if (linkStates[successeur].ligne < 0 && aff_hor.cmap[succ_type] > 0 && linkStates[successeur].temps < aff_hor.tmapmax)
                    {
                        bool test_periode = false;

                        if (linkStates[successeur].services.Count > 0)
                        {
                            int decal_jour = (int)Math.Floor(horaire / 1440f);
                            int kk;
                            for (kk = 0; kk < linkStates[successeur].services.Count; kk++)
                            {
                                if (decal_jour <= aff_hor.nb_jours)
                                {
                                    if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && linkStates[successeur].services[kk].hd + 1440f * decal_jour <= horaire && linkStates[successeur].services[kk].hf + 1440f * decal_jour > horaire)
                                    {
                                        test_periode = true;
                                        linkStates[successeur].service = kk;
                                    }
                                }
                            }
                        }
                        else
                        {
                            test_periode = true;
                        }

                        if (test_periode == true)
                        {
                            linkStates[successeur].touche = 1;
                            linkStates[successeur].cout = linkStates[successeur].temps * aff_hor.coef_tmap[succ_type] * aff_hor.cmap[succ_type] + linkStates[successeur].toll * aff_hor.ctoll[succ_type];
                            linkStates[successeur].l = linkStates[successeur].longueur;
                            linkStates[successeur].tmap = linkStates[successeur].temps * aff_hor.coef_tmap[succ_type];
                            linkStates[successeur].ttoll = linkStates[successeur].toll;
                            linkStates[successeur].h = horaire + linkStates[successeur].temps * aff_hor.coef_tmap[succ_type];
                            linkStates[successeur].pivot = -1;
                            linkStates[successeur].turn_pivot = -1;
                            linkStates[successeur].pole = depart;
                            linkStates[successeur].poleV2 = "";

                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                            while (bucket >= gga_nq.Count)
                                gga_nq.Add(new List<int>());
                            gga_nq[bucket].Add(successeur);
                            aff_hor.nb_pop++;
                        }
                    }
                    // TC
                    else if (aff_hor.cveh[succ_type] > 0)
                    {
                        int ii, jj, num_service = -1, h3 = 0, delta, duree_periode;
                        float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                        for (ii = 0; ii < linkStates[successeur].services.Count; ii++)
                        {
                            delta = 0;
                            duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[ii].regime].Length;
                            if ((linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f < horaire) || projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[ii].regime].Substring(jour, 1) == "N")
                            {
                                h1 = 1e38f;
                                h2 = 1e38f;
                                h3 = -1;
                                for (jj = jour + 1; jj <= Math.Min(jour + aff_hor.nb_jours, duree_periode - 1); jj++)
                                {
                                    if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[ii].regime].Substring(jj, 1) == "O" && (linkStates[successeur].services[ii].hd + (-jour + jj) * 24f * 60f < h1))
                                    {
                                        h1 = linkStates[successeur].services[ii].hd + (-jour + jj) * 24f * 60f;
                                        h2 = (-jour + jj);
                                        h3 = jj;
                                    }
                                }
                                if (h3 != -1)
                                {
                                    linkStates[successeur].services[ii].delta = h2;
                                }
                                else
                                {
                                    delta = -1;
                                }
                            }

                            if (linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f - horaire < max_correspondance)
                            {
                                if (((linkStates[successeur].services[ii].hf - linkStates[successeur].services[ii].hd) * aff_hor.cveh[succ_type] + (linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f - horaire) * aff_hor.cwait[succ_type]) + linkStates[successeur].toll * aff_hor.ctoll[succ_type] < cout2 && delta > -1)
                                {
                                    cout2 = (linkStates[successeur].services[ii].hf - linkStates[successeur].services[ii].hd) * aff_hor.cveh[succ_type] + (linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f - horaire) * aff_hor.cwait[succ_type] + linkStates[successeur].toll * aff_hor.ctoll[succ_type];
                                    num_service = ii;
                                }
                            }
                        }
                        if (num_service != -1)
                        {
                            linkStates[successeur].service = num_service;
                            linkStates[successeur].cout = (linkStates[successeur].services[num_service].hf - linkStates[successeur].services[num_service].hd) * aff_hor.cveh[succ_type] + (linkStates[successeur].services[num_service].hd + linkStates[successeur].services[num_service].delta * 1440f - horaire) * aff_hor.cwait[succ_type] + linkStates[successeur].toll * aff_hor.ctoll[succ_type];

                            linkStates[successeur].touche = 1;
                            linkStates[successeur].h = linkStates[successeur].services[linkStates[successeur].service].hf + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f;
                            linkStates[successeur].tatt = linkStates[successeur].services[linkStates[successeur].service].hd + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f - horaire;
                            linkStates[successeur].tatt1 = linkStates[successeur].services[linkStates[successeur].service].hd + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f - horaire;

                            linkStates[successeur].tveh = linkStates[successeur].services[linkStates[successeur].service].hf - linkStates[successeur].services[linkStates[successeur].service].hd;
                            linkStates[successeur].tcor = 0;
                            linkStates[successeur].ncorr = 1;
                            linkStates[successeur].tmap = 0;
                            linkStates[successeur].ttoll = linkStates[successeur].toll;
                            linkStates[successeur].l = linkStates[successeur].longueur;

                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                            while (bucket >= gga_nq.Count)
                                gga_nq.Add(new List<int>());
                            gga_nq[bucket].Add(successeur);
                            aff_hor.nb_pop++;
                            linkStates[successeur].pivot = -1;
                            linkStates[successeur].turn_pivot = -1;
                            linkStates[successeur].pole = projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;
                            linkStates[successeur].poleV2 = projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;
                        }
                    }
                }
            }

            int bucket_cout_max = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(aff_hor.temps_max / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));

            while (gga_nq.Count >= id_bucket && bucket_cout_max >= id_bucket)
            {
                while (gga_nq[id_bucket].Count == 0)
                {
                    id_bucket++;
                    if (id_bucket == gga_nq.Count || id_bucket == bucket_cout_max)
                        goto fin_gga1;
                }

                if (aff_hor.algorithme == 0)
                {
                    pivot = gga_nq[id_bucket][0];
                    gga_nq[id_bucket].RemoveAt(0);
                }
                else
                {
                    int k, id_pivot = -1;
                    double cout_max = 1e38f;
                    for (k = 0; k < gga_nq[id_bucket].Count; k++)
                    {
                        if (linkStates[gga_nq[id_bucket][k]].cout < cout_max)
                        {
                            cout_max = linkStates[gga_nq[id_bucket][k]].cout;
                            id_pivot = k;
                        }
                    }
                    pivot = gga_nq[id_bucket][id_pivot];
                    gga_nq[id_bucket].RemoveAt(id_pivot);
                    linkStates[pivot].touche = 3;
                }

                for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[linkStates[pivot].nd].succ.Count; j++)
                {
                    link troncon_succ = linkStates[projet.reseaux[projet.reseau_actif].nodes[linkStates[pivot].nd].succ[j]];
                    link troncon_pivot = linkStates[pivot];
                    successeur = projet.reseaux[projet.reseau_actif].nodes[linkStates[pivot].nd].succ[j];
                    succ_type = linkStates[successeur].type;

                    if (aff_hor.demitours == true)
                    {
                        if (troncon_pivot.no == troncon_succ.nd)
                            penalite = -1;
                        else
                            penalite = 0;
                    }
                    else
                    {
                        penalite = 0;
                    }

                    Turn virage = new Turn();
                    virage.arci = pivot;
                    virage.arcj = successeur;
                    float value2;
                    if (projet.reseaux[projet.reseau_actif].nodes[troncon_pivot.nd].is_intersection == true)
                    {
                        if (turns_global.TryGetValue(virage, out value2) == true)
                            penalite = turns_global[virage];
                        else
                            penalite = 0;
                    }

                    if (penalite >= 0)
                    {
                        if (penalite > 0)
                        {
                            temps_correspondance = penalite;
                            max_correspondance = aff_hor.tboa_max[succ_type];
                        }
                        else
                        {
                            temps_correspondance = aff_hor.tboa[succ_type];
                            max_correspondance = aff_hor.tboa_max[succ_type];
                        }

                        // Nouveau successeur
                        if (linkStates[successeur].touche == 0)
                        {
                            // Marche à pied
                            if (linkStates[successeur].ligne < 0 && aff_hor.cmap[succ_type] > 0 && linkStates[pivot].tmap + linkStates[successeur].temps < aff_hor.tmapmax)
                            {
                                bool test_periode = false;
                                linkStates[successeur].service = -1;
                                if (linkStates[successeur].services.Count > 0)
                                {
                                    int decal_jour = (int)(Math.Floor((linkStates[pivot].h + penalite) / 1440f));
                                    for (int kk = 0; kk < linkStates[successeur].services.Count; kk++)
                                    {
                                        if (decal_jour <= aff_hor.nb_jours)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && linkStates[successeur].services[kk].hd + 1440f * decal_jour <= linkStates[pivot].h + penalite && linkStates[successeur].services[kk].hf + 1440f * decal_jour > linkStates[pivot].h + penalite)
                                            {
                                                test_periode = true;
                                                linkStates[successeur].service = kk;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    test_periode = true;
                                }

                                if (test_periode == true)
                                {
                                    linkStates[successeur].cout = linkStates[pivot].cout + (linkStates[successeur].temps + penalite) * aff_hor.coef_tmap[succ_type] * aff_hor.cmap[succ_type]+ projet.reseaux[projet.reseau_actif].link[successeur].toll * aff_hor.ctoll[pred_type];
                                    linkStates[successeur].h = linkStates[pivot].h + (linkStates[successeur].temps) * aff_hor.coef_tmap[succ_type] + penalite;
                                    linkStates[successeur].tatt = linkStates[pivot].tatt;
                                    linkStates[successeur].tatt1 = linkStates[pivot].tatt1;
                                    linkStates[successeur].tveh = linkStates[pivot].tveh;
                                    linkStates[successeur].tcor = linkStates[pivot].tcor;
                                    linkStates[successeur].ncorr = linkStates[pivot].ncorr;
                                    linkStates[successeur].tmap = linkStates[pivot].tmap + (penalite + linkStates[successeur].temps) * aff_hor.coef_tmap[succ_type];
                                    linkStates[successeur].ttoll = linkStates[pivot].ttoll + linkStates[successeur].toll;
                                    linkStates[successeur].touche = 1;
                                    linkStates[successeur].l = linkStates[pivot].l + linkStates[successeur].longueur;

                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                    while (bucket >= gga_nq.Count)
                                        gga_nq.Add(new List<int>());
                                    gga_nq[bucket].Add(successeur);
                                    aff_hor.nb_pop++;
                                    linkStates[successeur].pivot = pivot;
                                    linkStates[successeur].turn_pivot = j;
                                    linkStates[successeur].pole = linkStates[pivot].pole;
                                    if (linkStates[pivot].ligne > 0)
                                        linkStates[successeur].poleV2 = linkStates[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;
                                    else
                                        linkStates[successeur].poleV2 = linkStates[pivot].poleV2;
                                }
                            }
                            // TC même ligne
                            else if (linkStates[successeur].ligne == linkStates[pivot].ligne && aff_hor.cveh[succ_type] > 0 && linkStates[pivot].ligne > 0)
                            {
                                int ii, num_service = -1;
                                for (ii = 0; ii < linkStates[successeur].services.Count; ii++)
                                {
                                    if (linkStates[successeur].services[ii].numero == linkStates[pivot].services[linkStates[pivot].service].numero)
                                    {
                                        if (linkStates[successeur].services[ii].hd >= linkStates[pivot].services[linkStates[pivot].service].hf)
                                        {
                                            num_service = ii;
                                        }
                                    }
                                }
                                if (num_service != -1 && linkStates[successeur].services[num_service].hd + linkStates[pivot].services[linkStates[pivot].service].delta * 1440f >= linkStates[pivot].h)
                                {
                                    linkStates[successeur].service = num_service;
                                    linkStates[successeur].services[num_service].delta = linkStates[pivot].services[linkStates[pivot].service].delta;
                                    linkStates[successeur].touche = 1;
                                    linkStates[successeur].cout = linkStates[pivot].cout + (linkStates[successeur].services[linkStates[successeur].service].hf + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f - linkStates[pivot].h) * aff_hor.cveh[succ_type] + linkStates[successeur].toll * aff_hor.ctoll[succ_type];
                                    linkStates[successeur].h = linkStates[successeur].services[linkStates[successeur].service].hf + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f;
                                    linkStates[successeur].tatt = linkStates[pivot].tatt;
                                    linkStates[successeur].tatt1 = linkStates[pivot].tatt1;
                                    linkStates[successeur].tveh = linkStates[pivot].tveh + linkStates[successeur].services[linkStates[successeur].service].hf + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f - linkStates[pivot].h;
                                    linkStates[successeur].tcor = linkStates[pivot].tcor;
                                    linkStates[successeur].ncorr = linkStates[pivot].ncorr;
                                    linkStates[successeur].l = linkStates[pivot].l + linkStates[successeur].longueur;
                                    linkStates[successeur].tmap = linkStates[pivot].tmap;
                                    linkStates[successeur].ttoll = linkStates[pivot].ttoll + linkStates[successeur].toll;

                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                    while (bucket >= gga_nq.Count)
                                        gga_nq.Add(new List<int>());
                                    gga_nq[bucket].Add(successeur);
                                    aff_hor.nb_pop++;
                                    linkStates[successeur].pivot = pivot;
                                    linkStates[successeur].turn_pivot = j;
                                    linkStates[successeur].pole = linkStates[pivot].pole;
                                    linkStates[successeur].poleV2 = linkStates[pivot].poleV2;
                                }
                            }
                            // TC lignes différentes
                            else if (linkStates[successeur].ligne != linkStates[pivot].ligne && aff_hor.cveh[succ_type] > 0 && linkStates[successeur].ligne > 0)
                            {
                                int ii, jj, num_service = -1, h3 = 0, delta, duree_periode;
                                float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;

                                for (ii = 0; ii < linkStates[successeur].services.Count; ii++)
                                {
                                    delta = 0;
                                    duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[ii].regime].Length;

                                    if ((linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f < linkStates[pivot].h + temps_correspondance) || projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[ii].regime].Substring(jour, 1) == "N")
                                    {
                                        h1 = 1e38f;
                                        h2 = 1e38f;
                                        h3 = -1;
                                        for (jj = jour + 1; jj <= Math.Min(jour + aff_hor.nb_jours, duree_periode - 1); jj++)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[ii].regime].Substring(jj, 1) == "O" && (linkStates[successeur].services[ii].hd + (-jour + jj) * 24f * 60f < h1) && (linkStates[successeur].services[ii].hd + (-jour + jj) * 24f * 60f) - temps_correspondance > linkStates[pivot].h)
                                            {
                                                h1 = linkStates[successeur].services[ii].hd + (-jour + jj) * 24f * 60f;
                                                h2 = (-jour + jj);
                                                h3 = jj;
                                            }
                                        }
                                        if (h3 != -1)
                                        {
                                            if (linkStates[successeur].services[ii].delta > h2 || linkStates[successeur].touche == 0)
                                            {
                                                linkStates[successeur].services[ii].delta = h2;
                                            }
                                        }
                                        else
                                        {
                                            delta = -1;
                                        }
                                    }

                                    if ((linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f < linkStates[pivot].h + max_correspondance) && (linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f >= linkStates[pivot].h + temps_correspondance))
                                    {
                                        if (linkStates[pivot].cout + (linkStates[successeur].services[ii].hf - linkStates[successeur].services[ii].hd) * aff_hor.cveh[succ_type] + (linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f - linkStates[pivot].h) * aff_hor.cwait[succ_type] + temps_correspondance * aff_hor.cboa[succ_type] + linkStates[successeur].toll * aff_hor.ctoll[succ_type] < cout2 && delta > -1)
                                        {
                                            cout2 = linkStates[pivot].cout + (linkStates[successeur].services[ii].hf - linkStates[successeur].services[ii].hd) * aff_hor.cveh[succ_type] + (linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f - linkStates[pivot].h) * aff_hor.cwait[succ_type] + temps_correspondance * aff_hor.cboa[succ_type] + linkStates[successeur].toll * aff_hor.ctoll[succ_type];
                                            num_service = ii;
                                        }
                                    }
                                }
                                if (num_service != -1)
                                {
                                    linkStates[successeur].service = num_service;
                                    linkStates[successeur].cout = linkStates[pivot].cout + (linkStates[successeur].services[num_service].hf - linkStates[successeur].services[num_service].hd) * aff_hor.cveh[succ_type] + (linkStates[successeur].services[num_service].hd + linkStates[successeur].services[num_service].delta * 1440f - linkStates[pivot].h) * aff_hor.cwait[succ_type] + (temps_correspondance * aff_hor.cboa[succ_type]) + linkStates[successeur].toll * aff_hor.ctoll[succ_type];

                                    linkStates[successeur].touche = 1;
                                    linkStates[successeur].h = linkStates[successeur].services[linkStates[successeur].service].hf + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f;
                                    if (linkStates[pivot].ncorr == 0)
                                    {
                                        linkStates[successeur].tatt1 = linkStates[successeur].services[linkStates[successeur].service].hd + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f - linkStates[pivot].h;
                                    }
                                    else
                                    {
                                        linkStates[successeur].tatt1 = linkStates[pivot].tatt1;
                                    }

                                    linkStates[successeur].tatt = linkStates[pivot].tatt + linkStates[successeur].services[linkStates[successeur].service].hd + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f - linkStates[pivot].h;
                                    linkStates[successeur].tveh = linkStates[pivot].tveh + linkStates[successeur].services[linkStates[successeur].service].hf - linkStates[successeur].services[linkStates[successeur].service].hd;
                                    linkStates[successeur].tcor = linkStates[pivot].tcor + temps_correspondance;
                                    linkStates[successeur].ncorr = linkStates[pivot].ncorr + 1;
                                    linkStates[successeur].l = linkStates[pivot].l + linkStates[successeur].longueur;
                                    linkStates[successeur].tmap = linkStates[pivot].tmap;
                                    linkStates[successeur].ttoll = linkStates[pivot].ttoll + linkStates[successeur].toll;

                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                    while (bucket >= gga_nq.Count)
                                        gga_nq.Add(new List<int>());
                                    gga_nq[bucket].Add(successeur);
                                    aff_hor.nb_pop++;
                                    linkStates[successeur].pivot = pivot;
                                    linkStates[successeur].turn_pivot = j;

                                    if (linkStates[pivot].ligne > 0)
                                    {
                                        linkStates[successeur].poleV2 = linkStates[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;
                                    }
                                    else
                                    {
                                        linkStates[successeur].poleV2 = linkStates[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;
                                    }

                                    if (linkStates[pivot].pole == depart)
                                    {
                                        linkStates[successeur].pole = projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;
                                    }
                                    else
                                    {
                                        linkStates[successeur].pole = linkStates[pivot].pole;
                                    }
                                }
                            }
                        }
                        // Éléments déjà touchés
                        else if (linkStates[successeur].touche == 1 || linkStates[successeur].touche == 2)
                        {
                            // Code pour MAJ des éléments déjà touchés (simplifié ici)
                             int id_service = -1;
                                                //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets)));
                                                //successeurs marche à pied
                                                if (linkStates[successeur].ligne < 0 && projet.param_affectation_horaire.cmap[succ_type] > 0 && linkStates[pivot].tmap + linkStates[successeur].temps < projet.param_affectation_horaire.tmapmax)
                                                {
                                                    bool test_periode = false;

                                                    if (linkStates[successeur].services.Count > 0)
                                                    {
                                                        int decal_jour = (int)(Math.Floor((linkStates[pivot].h + penalite) / 1440f));
                                                        for (int kk = 0; kk < linkStates[successeur].services.Count; kk++)
                                                        {
                                                            if (decal_jour <= projet.param_affectation_horaire.nb_jours)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && linkStates[successeur].services[kk].hd + 1440f * decal_jour <= linkStates[pivot].h + penalite && linkStates[successeur].services[kk].hf + 1440f * decal_jour > linkStates[pivot].h + penalite)
                                                                {
                                                                    test_periode = true;
                                                                    id_service = kk;

                                                                }
                                                            }
                                                        }

                                                    }
                                                    else
                                                    {
                                                        test_periode = true;
                                                    }
                                                    if (test_periode == true)
                                                    {

                                                        if (linkStates[successeur].cout > linkStates[pivot].cout + (penalite + linkStates[successeur].temps) * projet.param_affectation_horaire.coef_tmap[succ_type] * projet.param_affectation_horaire.cmap[succ_type] + linkStates[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type])
                                                        {
                                                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets)));
                                                            gga_nq[bucket].Remove(successeur);
                                                            linkStates[successeur].cout = linkStates[pivot].cout + (linkStates[successeur].temps + penalite) * projet.param_affectation_horaire.coef_tmap[succ_type] * projet.param_affectation_horaire.cmap[succ_type] + linkStates[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                            linkStates[successeur].h = linkStates[pivot].h + (linkStates[successeur].temps) * projet.param_affectation_horaire.coef_tmap[succ_type] + penalite;
                                                            linkStates[successeur].tatt = linkStates[pivot].tatt;
                                                            linkStates[successeur].tatt1 = linkStates[pivot].tatt1;
                                                            linkStates[successeur].tveh = linkStates[pivot].tveh;
                                                            linkStates[successeur].tcor = linkStates[pivot].tcor;
                                                            linkStates[successeur].ncorr = linkStates[pivot].ncorr;
                                                            linkStates[successeur].tmap = linkStates[pivot].tmap + (penalite + linkStates[successeur].temps) * projet.param_affectation_horaire.coef_tmap[succ_type];
                                                            linkStates[successeur].ttoll = linkStates[pivot].ttoll + linkStates[successeur].toll;

                                                            linkStates[successeur].touche = 2;
                                                            linkStates[successeur].l = linkStates[pivot].l + linkStates[successeur].longueur;
                                                            linkStates[successeur].pivot = pivot;
                                                            linkStates[successeur].turn_pivot = j;
                                                            linkStates[successeur].pole = linkStates[pivot].pole;


                                                            if (linkStates[pivot].ligne > 0)
                                                            {
                                                                linkStates[successeur].poleV2 = linkStates[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;

                                                            }
                                                            else
                                                            {
                                                                linkStates[successeur].poleV2 = linkStates[pivot].poleV2;
                                                            }


                                                            linkStates[successeur].service = id_service;                                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), ;
                                                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets)));
                                                            gga_nq[bucket].Add(successeur);
                                                            projet.param_affectation_horaire.nb_pop++;
                                                        }
                                                    }

                                                }
                                                //successeurs TC même ligne
                                                else if ((linkStates[successeur].ligne == linkStates[pivot].ligne && projet.param_affectation_horaire.cveh[succ_type] > 0 && linkStates[pivot].ligne > 0 && linkStates[successeur].cout > linkStates[pivot].cout))
                                                {
                                                    int ii, num_service = -1;
                                                    for (ii = 0; ii < linkStates[successeur].services.Count; ii++)
                                                    {
                                                        if (linkStates[successeur].services[ii].numero == linkStates[pivot].services[linkStates[pivot].service].numero)
                                                        {
                                                            if (linkStates[successeur].services[ii].hd >= linkStates[pivot].services[linkStates[pivot].service].hf)
                                                            {
                                                                num_service = ii;
                                                            }
                                                        }


                                                    }

                                                    if (num_service != -1)
                                                    {
                                                        //                                                    if (linkStates[successeur].services[num_service].hd + linkStates[pivot].services[linkStates[pivot].service].delta * 1440f >= linkStates[pivot].h)
                                                        if (linkStates[successeur].services[num_service].hd >= linkStates[pivot].services[linkStates[pivot].service].hf)
                                                        {

                                                            if (linkStates[successeur].cout > linkStates[pivot].cout + (linkStates[successeur].services[num_service].hf + linkStates[pivot].services[linkStates[pivot].service].delta * 1440f - linkStates[pivot].h) * projet.param_affectation_horaire.cveh[succ_type] + linkStates[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type] && linkStates[successeur].services[num_service].hd >= linkStates[pivot].services[linkStates[pivot].service].hf)
                                                            {
                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets)));
                                                                gga_nq[bucket].Remove(successeur);
                                                                linkStates[successeur].services[num_service].delta = linkStates[pivot].services[linkStates[pivot].service].delta;
                                                                linkStates[successeur].service = num_service;
                                                                linkStates[successeur].touche = 2;
                                                                linkStates[successeur].cout = linkStates[pivot].cout + (linkStates[successeur].services[linkStates[successeur].service].hf + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f - linkStates[pivot].h) * projet.param_affectation_horaire.cveh[succ_type] + linkStates[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                                linkStates[successeur].h = linkStates[successeur].services[linkStates[successeur].service].hf + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f;
                                                                linkStates[successeur].tatt = linkStates[pivot].tatt;
                                                                linkStates[successeur].tatt1 = linkStates[pivot].tatt1;
                                                                linkStates[successeur].tveh = linkStates[pivot].tveh + linkStates[successeur].services[linkStates[successeur].service].hf /* + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f */- linkStates[pivot].h;
                                                                linkStates[successeur].tcor = linkStates[pivot].tcor;
                                                                linkStates[successeur].ncorr = linkStates[pivot].ncorr;
                                                                linkStates[successeur].l = linkStates[pivot].l + linkStates[successeur].longueur;
                                                                linkStates[successeur].tmap = linkStates[pivot].tmap;
                                                                linkStates[successeur].ttoll = linkStates[pivot].ttoll + linkStates[successeur].toll;

                                                                linkStates[successeur].pivot = pivot;

                                                                linkStates[successeur].turn_pivot = j;
                                                                linkStates[successeur].pole = linkStates[pivot].pole;
                                                                linkStates[successeur].poleV2 = linkStates[pivot].poleV2;
                                                                //bucket = Convert.ToInt32(Math.Min((Math.Pow(linkStates[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets)));
                                                                gga_nq[bucket].Add(successeur);
                                                                projet.param_affectation_horaire.nb_pop++;
                                                            }
                                                        }
                                                    }
                                                }
                                                //successeurs TC lignes différentes
                                                else if ((linkStates[successeur].ligne != linkStates[pivot].ligne) && /*linkStates[successeur].ligne>0 && linkStates[pivot].ligne > 0 && */projet.param_affectation_horaire.cveh[succ_type] > 0 && linkStates[successeur].cout > linkStates[pivot].cout)//&& (linkStates[pivot].h + projet.param_affectation_horaire.tboa < linkStates[successeur].services[linkStates[successeur].service].hd + linkStates[successeur].services[linkStates[successeur].service].delta*1440f))
                                                {
                                                    int ii, jj, num_service = -1, h3 = -1, duree_periode, delta;
                                                    float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                                    for (ii = 0; ii < linkStates[successeur].services.Count; ii++)
                                                    {
                                                        delta = 0;
                                                        //linkStates[successeur].services[ii].delta = 0;
                                                        duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[ii].regime].Length;

                                                        if ((linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f < linkStates[pivot].h + temps_correspondance) || projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[ii].regime].Substring(jour, 1) == "N")
                                                        {

                                                            h1 = 1e38f;
                                                            h2 = 1e38f;
                                                            h3 = -1;
                                                            for (jj = jour + 1; jj <= Math.Min(jour + projet.param_affectation_horaire.nb_jours, duree_periode - 1); jj++)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates[successeur].services[ii].regime].Substring(jj, 1) == "O" && (linkStates[successeur].services[ii].hd + (-jour + jj) * 24f * 60f) < h1 && (linkStates[successeur].services[ii].hd + (-jour + jj) * 24f * 60f - temps_correspondance) > linkStates[pivot].h)
                                                                {
                                                                    h1 = linkStates[successeur].services[ii].hd + (-jour + jj) * 24f * 60f;
                                                                    h2 = (-jour + jj);
                                                                    h3 = jj;
                                                                }

                                                            }
                                                            if (h3 != -1)
                                                            {
                                                                if (linkStates[successeur].services[ii].delta < h2 || linkStates[successeur].touche == 0)
                                                                {
                                                                    linkStates[successeur].services[ii].delta = h2;
                                                                }


                                                            }
                                                            else
                                                            {
                                                                delta = -1;
                                                            }


                                                        }
                                                        if ((linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f < linkStates[pivot].h + max_correspondance) && (linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f >= linkStates[pivot].h + temps_correspondance))
                                                        {
                                                            if (linkStates[pivot].cout + (linkStates[successeur].services[ii].hf - linkStates[successeur].services[ii].hd) * projet.param_affectation_horaire.cveh[succ_type] + (linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f - linkStates[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[succ_type]) + linkStates[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type] < cout2 && delta > -1)
                                                            {
                                                                cout2 = linkStates[pivot].cout + (linkStates[successeur].services[ii].hf - linkStates[successeur].services[ii].hd) * projet.param_affectation_horaire.cveh[succ_type] + (linkStates[successeur].services[ii].hd + linkStates[successeur].services[ii].delta * 1440f - linkStates[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[succ_type]) + linkStates[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                                num_service = ii;
                                                            }
                                                        }

                                                    }
                                                    if (num_service != -1)
                                                    {
                                                        if (linkStates[successeur].cout > linkStates[pivot].cout + (linkStates[successeur].services[num_service].hf - linkStates[successeur].services[num_service].hd) * projet.param_affectation_horaire.cveh[succ_type] + (linkStates[successeur].services[num_service].hd + linkStates[successeur].services[num_service].delta * 1440f - linkStates[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[succ_type]) + linkStates[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type])
                                                        {
                                                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets)));
                                                            gga_nq[bucket].Remove(successeur);
                                                            linkStates[successeur].service = num_service;
                                                            linkStates[successeur].cout = linkStates[pivot].cout + (linkStates[successeur].services[num_service].hf - linkStates[successeur].services[num_service].hd) * projet.param_affectation_horaire.cveh[succ_type] + (linkStates[successeur].services[num_service].hd + linkStates[successeur].services[num_service].delta * 1440f - linkStates[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[succ_type]) + linkStates[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                            linkStates[successeur].touche = 2;

                                                            linkStates[successeur].h = linkStates[successeur].services[linkStates[successeur].service].hf + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f;
                                                            if (linkStates[pivot].ncorr == 0)
                                                            {
                                                                linkStates[successeur].tatt1 = linkStates[successeur].services[linkStates[successeur].service].hd + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f - linkStates[pivot].h;
                                                            }
                                                            else
                                                            {
                                                                linkStates[successeur].tatt1 = linkStates[pivot].tatt1;
                                                            }
                                                            linkStates[successeur].tatt = linkStates[pivot].tatt + linkStates[successeur].services[linkStates[successeur].service].hd + linkStates[successeur].services[linkStates[successeur].service].delta * 1440f - linkStates[pivot].h;
                                                            linkStates[successeur].tveh = linkStates[pivot].tveh + linkStates[successeur].services[linkStates[successeur].service].hf - linkStates[successeur].services[linkStates[successeur].service].hd;
                                                            linkStates[successeur].tcor = linkStates[pivot].tcor + temps_correspondance;
                                                            linkStates[successeur].ncorr = linkStates[pivot].ncorr + 1;
                                                            linkStates[successeur].l = linkStates[pivot].l + linkStates[successeur].longueur;
                                                            linkStates[successeur].tmap = linkStates[pivot].tmap;
                                                            linkStates[successeur].ttoll = linkStates[pivot].ttoll + linkStates[successeur].toll;

                                                            linkStates[successeur].pivot = pivot;
                                                            linkStates[successeur].turn_pivot = j;
                                                            if (linkStates[pivot].pole == depart)
                                                            {
                                                                linkStates[successeur].pole = projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;
                                                            }
                                                            else
                                                            {
                                                                linkStates[successeur].pole = linkStates[pivot].pole;
                                                            }
                                                            if (linkStates[pivot].ligne > 0)
                                                            {

                                                                linkStates[successeur].poleV2 = linkStates[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;
                                                            }
                                                            else
                                                            {

                                                                linkStates[successeur].poleV2 = linkStates[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates[successeur].no].i;
                                                            }


                                                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets)));
                                                            gga_nq[bucket].Add(successeur);
                                                            projet.param_affectation_horaire.nb_pop++;
                                                        }
                                                    }
                        }
                    }
                }
            }

        fin_gga1:
            // Trouver l'arrivée
            int arrivee = -1;
            double cout_fin = 1e38f;
            if (projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(q, out value) == true)
            {
                for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[q]].pred.Count; j++)
                {
                    int predecesseur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[q]].pred[j];
                    if (linkStates[predecesseur].touche != 0 && linkStates[predecesseur].cout < cout_fin)
                    {
                        arrivee = predecesseur;
                        cout_fin = linkStates[predecesseur].cout;
                    }
                }
            }

            if (arrivee != -1)
            {
                if (linkStates[arrivee].ligne > 0)
                {
                    if (!result.LinkAffectations.ContainsKey(arrivee))
                        result.LinkAffectations[arrivee] = new LinkAffectation();
                    result.LinkAffectations[arrivee].alij += od;
                    result.LinkAffectations[arrivee].ServiceAffectations[linkStates[arrivee].service] = new ServiceAffectation { volau = od };
                }
            }

            // Accumuler les résultats le long du chemin
            pivot = arrivee;
            while (pivot != -1)
            {
                if (!result.LinkAffectations.ContainsKey(pivot))
                    result.LinkAffectations[pivot] = new LinkAffectation();

                result.LinkAffectations[pivot].volau += od;

                if (linkStates[pivot].pivot != -1 && aff_hor.sortie_turns == true)
                {
                    Turn virage = new Turn();
                    virage.arci = linkStates[pivot].pivot;
                    virage.arcj = pivot;
                    if (result.Transfers.ContainsKey(virage))
                        result.Transfers[virage] += od;
                    else
                        result.Transfers[virage] = od;
                }

                if (linkStates[pivot].service >= 0)
                {
                    if (!result.LinkAffectations[pivot].ServiceAffectations.ContainsKey(linkStates[pivot].service))
                        result.LinkAffectations[pivot].ServiceAffectations[linkStates[pivot].service] = new ServiceAffectation();
                    result.LinkAffectations[pivot].ServiceAffectations[linkStates[pivot].service].volau += od;
                }

                if (linkStates[pivot].pivot == -1)
                {
                    if (linkStates[pivot].ligne > 0)
                    {
                        result.LinkAffectations[pivot].boai += od;
                        if (linkStates[pivot].service >= 0)
                        {
                            if (!result.LinkAffectations[pivot].ServiceAffectations.ContainsKey(linkStates[pivot].service))
                                result.LinkAffectations[pivot].ServiceAffectations[linkStates[pivot].service] = new ServiceAffectation();
                            result.LinkAffectations[pivot].ServiceAffectations[linkStates[pivot].service].boat += od;
                        }
                    }
                }
                else if (linkStates[pivot].ligne != linkStates[linkStates[pivot].pivot].ligne)
                {
                    if (linkStates[pivot].ligne > 0)
                    {
                        result.LinkAffectations[pivot].boai += od;
                        if (linkStates[pivot].service >= 0)
                        {
                            if (!result.LinkAffectations[pivot].ServiceAffectations.ContainsKey(linkStates[pivot].service))
                                result.LinkAffectations[pivot].ServiceAffectations[linkStates[pivot].service] = new ServiceAffectation();
                            result.LinkAffectations[pivot].ServiceAffectations[linkStates[pivot].service].boat += od;
                        }
                    }
                    if (linkStates[linkStates[pivot].pivot].ligne > 0)
                    {
                        result.LinkAffectations[linkStates[pivot].pivot].alij += od;
                        if (linkStates[linkStates[pivot].pivot].service >= 0)
                        {
                            if (!result.LinkAffectations[linkStates[pivot].pivot].ServiceAffectations.ContainsKey(linkStates[linkStates[pivot].pivot].service))
                                result.LinkAffectations[linkStates[pivot].pivot].ServiceAffectations[linkStates[linkStates[pivot].pivot].service] = new ServiceAffectation();
                            result.LinkAffectations[linkStates[pivot].pivot].ServiceAffectations[linkStates[linkStates[pivot].pivot].service].alit += od;
                        }
                    }
                }

                pivot = linkStates[pivot].pivot;
            }
        }

        private static void ProcessODDirection2(string p, string q, int jour, float horaire, string libod, float od, etude projet, Param_affectation_horaire aff_hor, Dictionary<Turn, float> turns_global, HashSet<string> types, Dictionary<int, LinkInfo> linkStates, List<List<int>> gga_nq, ODGroupResult result)
        {
            // Version similaire pour le sens 2 (arrivée)
            // Code symétrique au sens 1 mais en sens inverse
            // ... (À adapter de manière symétrique)
            int i, j;
            string[] param = { ";" };

            // Initialiser les liens
            var linkStates = projet.reseaux[projet.reseau_actif].links
                .Select(l => l.Clone())
                .ToList();



            gga_nq.Clear();
            string depart = q;
            int pivot = -1, value;
            int bucket, id_bucket = 0, predecesseur;
            float penalite = 0, temps_correspondance, max_correspondance;


            HashSet<String> filtre = new HashSet<String>();
            if (aff_hor.texte_filtre_sortie.Trim().Length > 0)
            {
                string[] ch_filtre = aff_hor.texte_filtre_sortie.Split('|');
                for (int f = 0; f < ch_filtre.Length; f++)
                {
                    if (filtre.Contains(ch_filtre[f].Trim()) == false)
                        filtre.Add(ch_filtre[f].Trim());
                }
            }

            if (linkStates.numnoeud.TryGetValue(depart, out value) == true)
            {

                for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[linkStates.numnoeud[depart]].pred.Count; j++)
                {
                    predecesseur = projet.reseaux[projet.reseau_actif].nodes[linkStates.numnoeud[depart]].pred[j];
                    String pred_type = linkStates.links[predecesseur].type;
                    max_correspondance = aff_hor.tboa_max[pred_type];




                    if (linkStates.links[predecesseur].ligne < 0 && aff_hor.cmap[pred_type] > 0 && linkStates.links[predecesseur].temps < aff_hor.tmapmax)
                    {
                        bool test_periode = false;

                        if (linkStates.links[predecesseur].services.Count > 0)
                        {
                            int decal_jour = (int)Math.Floor(horaire / 1440f);
                            for (int kk = 0; kk < linkStates.links[predecesseur].services.Count; kk++)
                            {
                                if (Math.Abs(decal_jour) <= aff_hor.nb_jours)
                                {
                                    if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && linkStates.links[predecesseur].services[kk].hd + 1440f * decal_jour <= horaire && linkStates.links[predecesseur].services[kk].hf + 1440f * decal_jour > horaire)
                                    {
                                        test_periode = true;
                                       linkStates[predecesseur].service = kk;
                                    }
                                }
                            }

                        }
                        else
                        {
                            test_periode = true;
                        }

                        if (test_periode == true)
                        {


                            //touches.Enqueue(successeur);
                            linkStates[predecesseur].touche = 1;
                            linkStates[predecesseur].cout = (linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type] * aff_hor.cmap[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                            linkStates[predecesseur].tmap = (linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type];
                            linkStates[predecesseur].ttoll = linkStates.links[predecesseur].toll;

                            linkStates[predecesseur].h = horaire - (linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type];
                            linkStates[predecesseur].l = linkStates.links[predecesseur].longueur;
                            linkStates[predecesseur].pivot = -1;
                            linkStates[predecesseur].turn_pivot = -1;

                            linkStates[predecesseur].pole = depart;
                            linkStates[predecesseur].poleV2 = "";
                            //                                    bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout /aff_hor.param_dijkstra,aff_hor.pu), aff_hor.max_nb_buckets)));
                            while (bucket >= gga_nq.Count)
                            {
                                gga_nq.Add(new List<int>());
                            }
                            gga_nq[bucket].Add(predecesseur);
                            aff_hor.nb_pop++;
                        }
                    }
                    else if (aff_hor.hor.cveh[pred_type] > 0)
                    {
                        int ii, jj, num_service = -1, h3 = 0, delta, duree_periode;
                        float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                        for (ii = 0; ii < linkStates.links[predecesseur].services.Count; ii++)
                        {
                            delta = 0;
                            duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Length;

                            if ((linkStates.links[predecesseur].services[ii].hf > horaire) || projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                            {

                                h1 = -1e38f;
                                h2 = 1e38f;
                                h3 = -1;
                                for (jj = jour - 1; jj >= Math.Max(jour - aff_hor.nb_jours, 0); jj--)
                                {
                                    if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) < horaire)
                                    {
                                        h1 = linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                        h2 = (-jour + jj);
                                        h3 = jj;
                                    }

                                }
                                if (h3 != -1)
                                {
                                    linkStates.links[predecesseur].services[ii].delta = h2;
                                }
                                else
                                {
                                    delta = 1;
                                }


                            }

                            if (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + horaire < max_correspondance)
                            {
                                if (((linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + horaire) * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type]) < cout2 && delta < 1)
                                {
                                    cout2 = (linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) *aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + horaire) * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                    num_service = ii;

                                }
                            }

                        }
                        if (num_service != -1)
                        {
                            linkStates[predecesseur].service = num_service;
                            linkStates[predecesseur].cout = (linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].delta * 1440f + horaire) * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                            linkStates[predecesseur].touche = 1;

                            linkStates[predecesseur].h = linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hd + linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 60f * 24f;
                            linkStates[predecesseur].tatt = -linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 1440f + horaire;
                            linkStates[predecesseur].tatt1 = -linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 1440f + horaire;
                            linkStates[predecesseur].tveh = linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hd;
                            linkStates[predecesseur].tcor = 0;
                            linkStates[predecesseur].ncorr = 1;
                            linkStates[predecesseur].l = linkStates.links[predecesseur].longueur;
                            linkStates[predecesseur].tmap = 0;
                            linkStates[predecesseur].ttoll = linkStates.links[predecesseur].toll;

                            //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                            while (bucket >= gga_nq.Count)
                            {
                                gga_nq.Add(new List<int>());
                            }
                            gga_nq[bucket].Add(predecesseur);
                            aff_hor.nb_pop++;
                            //                                touches.Enqueue(successeur);
                            linkStates[predecesseur].pivot = -1;
                            linkStates[predecesseur].turn_pivot = -1;
                            linkStates[predecesseur].pole = projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i;
                            linkStates[predecesseur].poleV2 = projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i;
                        }
                    }

                }
            }

            int bucket_cout_max = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(aff_hor.temps_max / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));

            while (gga_nq.Count >= id_bucket && bucket_cout_max >= id_bucket)
            {

                while (gga_nq[id_bucket].Count == 0)
                {
                    id_bucket++;
                    if (id_bucket == gga_nq.Count + 1 || bucket_cout_max == id_bucket + 1)
                    {
                        goto fin_gga2;
                    }
                }
                if (aff_hor.algorithme == 0)
                {
                    pivot = gga_nq[id_bucket][0];
                    gga_nq[id_bucket].RemoveAt(0);

                }
                else
                {
                    int k, id_pivot = -1; double cout_max = 1e38f;
                    for (k = 0; k < gga_nq[id_bucket].Count; k++)
                    {
                        if (linkStates[gga_nq[id_bucket][k]].cout < cout_max)
                        {
                            cout_max = linkStates[gga_nq[id_bucket][k]].cout;
                            id_pivot = k;
                        }
                    }
                    pivot = gga_nq[id_bucket][id_pivot];
                    gga_nq[id_bucket].RemoveAt(id_pivot);
                    linkStates[pivot].touche = 3;
                }


                //avancement.textBox1.Text = touches.Count.ToString() + " " + calcules.Count.ToString() + " " + linkStates.links[pivot].cout;
                //avancement.textBox1.Refresh();
                for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[linkStates.links[pivot].no].pred.Count; j++)
                {

                    String pivot_type = linkStates.links[pivot].type;
                    link troncon_pred = linkStates.links[projet.reseaux[projet.reseau_actif].nodes[linkStates.links[pivot].no].pred[j]];
                    link troncon_pivot = linkStates.links[pivot];
                    predecesseur = projet.reseaux[projet.reseau_actif].nodes[linkStates.links[pivot].no].pred[j];
                    String pred_type = linkStates.links[predecesseur].type;

                    if (aff_hor.demitours == true)
                    {

                        if (troncon_pivot.nd == troncon_pred.no)
                        {

                            penalite = -1;
                        }
                        else
                        {
                            penalite = 0;
                        }
                    }

                    else
                    {
                        penalite = 0;
                    }
                    Turn virage = new Turn();
                    virage.arci = predecesseur;
                    virage.arcj = pivot;
                    float value2;
                    if (projet.reseaux[projet.reseau_actif].nodes[troncon_pivot.no].is_intersection == true)
                    {
                        if (turns.TryGetValue(virage, out value2) == true)
                        {
                            penalite = turns[virage];
                            pred_type = linkStates.links[predecesseur].type;
                        }
                        else
                        {
                            penalite = 0;
                        }
                    }



                    if (penalite >= 0)
                    {
                        if (penalite > 0)
                        {
                            temps_correspondance = penalite + aff_hor.tboa[pivot_type];
                            max_correspondance = aff_hor.tboa_max[pivot_type];

                        }
                        else
                        {
                            temps_correspondance = aff_hor.tboa[pivot_type];
                            max_correspondance = aff_hor.tboa_max[pivot_type];

                        }
                        //successeurs touches pour la première fois
                        if (linkStates[predecesseur].touche == 0)
                        {
                            // predecesseur marche à pied pivot marche
                            if (linkStates.links[predecesseur].ligne < 0 && linkStates.links[pivot].ligne < 0 && aff_hor.cmap[pred_type] > 0 && (linkStates.links[pivot].tmap + linkStates.links[predecesseur].temps < aff_hor.tmapmax))
                            {
                                bool test_periode = false;
                                linkStates[predecesseur].service = -1;
                                if (linkStates.links[predecesseur].services.Count > 0)
                                {
                                    int decal_jour = (int)(Math.Floor((linkStates.links[pivot].h - penalite) / 1440f));
                                    for (int kk = 0; kk < linkStates.links[predecesseur].services.Count; kk++)
                                    {
                                        if (Math.Abs(decal_jour) <= aff_hor.nb_jours)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && linkStates.links[predecesseur].services[kk].hd + 1440f * decal_jour <= linkStates[pivot].h - penalite && linkStates.links[predecesseur].services[kk].hf + 1440f * decal_jour > linkStates[pivot].h - penalite)
                                            {
                                                test_periode = true;
                                                li[predecesseur].service = kk;
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    test_periode = true;
                                }

                                if (test_periode == true)
                                {

                                    linkStates[predecesseur].cout = linkStates[pivot].cout + (linkStates.links[predecesseur].temps + penalite) * aff_hor.coef_tmap[pred_type] * aff_hor.cmap[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                    linkStates[predecesseur].h = linkStates.h - (linkStates.links[predecesseur].temps) * aff_hor[pred_type] - penalite;
                                    linkStates[predecesseur].tatt = linkStates[pivot].tatt;
                                    linkStates[predecesseur].tatt1 = linkStates[pivot].tatt1;
                                    linkStates[predecesseur].tveh = linkStates[pivot].tveh;
                                    linkStates[predecesseur].tcor = linkStates[pivot].tcor;
                                    linkStates[predecesseur].ncorr = linkStates[pivot].ncorr;
                                    linkStates[predecesseur].tmap = linkStates[pivot].tmap + (linkStates.links[predecesseur].temps + penalite) * aff_hor.coef_tmap[pred_type];
                                    linkStates[predecesseur].ttoll = linkStates[pivot].ttoll + linkStates.links[predecesseur].toll;

                                    linkStates[predecesseur].l = linkStates.links[pivot].l + linkStates.links[predecesseur].longueur;
                                    linkStates.touche = 1;

                                    //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout /aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                    while (bucket >= gga_nq.Count)
                                    {
                                        gga_nq.Add(new List<int>());
                                    }
                                    gga_nq[bucket].Add(predecesseur);
                                    aff_hor.nb_pop++;
                                    //                                        touches.Enqueue(successeur);
                                    linkStates[predecesseur].pivot = pivot;
                                    linkStates[predecesseur].turn_pivot = j;
                                    linkStates[predecesseur].pole = linkStates[pivot].pole;
                                    linkStates[predecesseur].poleV2 = linkStates[pivot].poleV2;
                                }
                            }
                            // predecesseur marche à pied pivot TC
                            else if (linkStates.links[predecesseur].ligne < 0 && linkStates.links[pivot].ligne > 0 && aff_hor.cmap[pred_type] > 0 && (linkStates.links[pivot].tmap + linkStates.links[predecesseur].temps < aff_hor.tmapmax))
                            {
                                bool test_periode = false;
                                linkStates[predecesseur].service = -1;


                                if (linkStates.links[predecesseur].services.Count > 0)
                                {
                                    int decal_jour = -(int)(Math.Floor((linkStates[pivot].h - temps_correspondance) / 1440f));
                                    for (int kk = 0; kk < linkStates.links[predecesseur].services.Count; kk++)
                                    {
                                        if (decal_jour <= a.nb_jours)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[kk].regime].Substring(jour - decal_jour, 1) == "O" && linkStates.links[predecesseur].services[kk].hd - 1440f * decal_jour <= linkStates[pivot].h - temps_correspondance && linkStates.links[predecesseur].services[kk].hf - 1440f * decal_jour > linkStates[pivot].h - temps_correspondance)
                                            {
                                                test_periode = true;
                                                linkStates[predecesseur].service = kk;
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    test_periode = true;
                                }

                                if (test_periode == true)
                                {


                                    linkStates[predecesseur].cout = linkStates[pivot].cout + (linkStates.links[predecesseur].temps + penalite) * aff_hor.coef_tmap[pred_type] * aff_hor.cmap[pred_type] +aff.cboa[pivot_type] * temps_correspondance + temps_correspondance * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                    linkStates[predecesseur].h = linkStates[pivot].h - (linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type] - temps_correspondance - penalite;
                                    linkStates[predecesseur].tatt = linkStates[pivot].tatt + temps_correspondance;
                                    linkStates[predecesseur].tatt1 = linkStates[pivot].tatt1;
                                    linkStates[predecesseur].tveh = linkStates[pivot].tveh;
                                    linkStates[predecesseur].tcor = linkStates[pivot].tcor + temps_correspondance;
                                    linkStates[predecesseur].ncorr = linkStates[pivot].ncorr + 1;
                                    linkStates[predecesseur].tmap = linkStates[pivot].tmap + (linkStates.links[predecesseur].temps + penalite) * aff_hor.coef_tmap[pred_type];
                                    linkStates[predecesseur].ttoll = linkStates[pivot].ttoll + linkStates.links[predecesseur].toll;

                                    linkStates[predecesseur].l = linkStates[pivot].l + linkStates.links[predecesseur].longueur;
                                    linkStates[predecesseur].touche = 1;
                                    // bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                    while (bucket >= gga_nq.Count)
                                    {
                                        gga_nq.Add(new List<int>());
                                    }
                                    gga_nq[bucket].Add(predecesseur);
                                    aff_hor.nb_pop++;
                                    //                                        touches.Enqueue(successeur);
                                    linkStates[predecesseur].pivot = pivot;
                                    linkStates.turn_pivot = j;
                                    linkStates.pole = projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i;
                                    linkStates[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i + linkStates[pivot].poleV2;
                                }
                            }
                            //predecesseurs TC même ligne
                            else if (linkStates.links[predecesseur].ligne == linkStates.links[pivot].ligne && linkStates.links[predecesseur].ligne > 0 && aff_hor.cveh[pred_type] > 0)
                            {
                                int ii, num_service = -1;
                                for (ii = 0; ii < linkStates.links[predecesseur].services.Count; ii++)
                                {
                                    if (linkStates.links[predecesseur].services[ii].numero == linkStates.links[pivot].services[linkStates.links[pivot].service].numero)
                                    {
                                        if (linkStates.links[predecesseur].services[ii].hf <= linkStates.links[pivot].services[linkStates.links[pivot].service].hd)
                                        {
                                            num_service = ii;
                                        }
                                    }
                                }
                                if (num_service != -1 && linkStates.links[predecesseur].services[num_service].hf + linkStates.links[pivot].services[linkStates.links[pivot].service].delta * 1440f <= linkStates[pivot].h)
                                {
                                    linkStates[predecesseur].service = num_service;
                                    linkStates[predecesseur].services[num_service].delta = linkStates.links[pivot].services[linkState[pivot].service].delta;

                                    linkStates[predecesseur].touche = 1;
                                    linkStates[predecesseur].cout =linkStates[pivot].cout + (-linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hd - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 1440f + linkStates[pivot].h) * aff_hor.cveh[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                    linkStates[predecesseur].h = linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hd + linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 60f * 24f;
                                    linkStates[predecesseur].tatt = linkStates[pivot].tatt;
                                    linkStates[predecesseur].tatt1 = linkStates[pivot].tatt1;
                                    linkStates[predecesseur].tveh = linkStates[pivot].tveh - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hd - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 1440f + linkStates[pivot].h;
                                    linkStates[predecesseur].tcor = linkStates[pivot].tcor;
                                    linkStates[predecesseur].ncorr = linkStates[pivot].ncorr;
                                    linkStates[predecesseur].l = linkStates[pivot].l + linkStates.links[predecesseur].longueur;
                                    linkStates[predecesseur].tmap = linkStates[pivot].tmap;
                                    linkStates[predecesseur].ttoll = linkStates[pivot].ttoll + linkStates.links[predecesseur].toll;

                                    //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                    while (bucket >= gga_nq.Count)
                                    {
                                        gga_nq.Add(new List<int>());
                                    }
                                    gga_nq[bucket].Add(predecesseur);
                                    aff_hor.nb_pop++;
                                    //touches.Enqueue(successeur);
                                    linkStates[predecesseur].pivot = pivot;
                                    linkStates[predecesseur].turn_pivot = j;
                                    linkStates[predecesseur].pole = linkStates[pivot].pole;
                                    linkStates[predecesseur].poleV2 = linkStates[pivot].poleV2;
                                }
                            }

                            //predecesseur TC lignes différentes
                            else if (linkStates.links[predecesseur].ligne != linkStates.links[pivot].ligne && linkStates.links[pivot].ligne > 0 && linkStates.links[predecesseur].ligne > 0 && aff_hor.cveh[pred_type] > 0)
                            {
                                int ii, jj, num_service = -1, h3 = 0, delta, duree_periode;
                                float h1 = -1e38f, h2 = 1e38f, cout2 = 1e38f;
                                for (ii = 0; ii < linkStates.links[predecesseur].services.Count; ii++)
                                {
                                    delta = 0;
                                    duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Length;

                                    if ((linkStates.links[predecesseur].services[ii].hf + temps_correspondance >linkStates[pivot].h) || projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                                    {

                                        h1 = -1e38f;
                                        h2 = 1e38f;
                                        h3 = -1;
                                        for (jj = jour - 1; jj >= Math.Max(jour - aff_hor.nb_jours, 0); jj--)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) + temps_correspondance < linkStates[pivot].h)
                                            {
                                                h1 = linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                                h2 = (-jour + jj);
                                                h3 = jj;
                                            }

                                        }
                                        if (h3 != -1)
                                        {
                                            if (linkStates.links[predecesseur].services[ii].delta > h2 || linkStates[predecesseur].touche == 0)
                                            {
                                                linkStates.links[predecesseur].services[ii].delta = h2;
                                            }
                                        }
                                        else
                                        {
                                            delta = 1;
                                        }


                                    }
                                    if ((linkStates.links[predecesseur].services[ii].hf + linkStates.links[predecesseur].services[ii].delta * 1440f + max_correspondance > linkStates[pivot].h) && (linkStates.links[predecesseur].services[ii].hf + linkStates.links[predecesseur].services[ii].delta * 1440f + temps_correspondance <= linkStates[pivot].h))
                                    {
                                        if ((linkStates[pivot].cout + (linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + temps_correspondance * aff_hor.cboa[pivot_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type]) < cout2 && delta < 1)
                                        {

                                            cout2 = linkStates[pivot].cout + (linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + temps_correspondance * aff_hor.cboa[pivot_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                            num_service = ii;

                                        }
                                    }

                                }
                                if (num_service != -1)
                                {
                                    linkStates[predecesseur].service = num_service;
                                    linkStates[predecesseur].cout = linkStates[pivot].cout + (linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].delta * 1440f + linkStates[pivot].h) * aff_hor.cwait[pivot_type] + (temps_correspondance * aff_hor.cboa[pivot_type]) + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];

                                    linkStates[predecesseur].touche = 1;

                                    linkStates[predecesseur].h = linkStates.links[predecesseur].services[linkStates[predecesseur].service].hd + linkStates.links[predecesseur].services[linkStates[predecesseur].service].delta * 60f * 24f;
                                    if (linkStates.links[pivot].ncorr == 0)
                                    {
                                        linkStates[predecesseur].tatt1 = -linkStates.links[predecesseur].services[linkStates[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates[predecesseur].service].delta * 1440f + linkStates[pivot].h;
                                    }
                                    else
                                    {
                                        linkStates[predecesseur].tatt1 = linkStates[pivot].tatt1;
                                    }

                                    linkStates[predecesseur].tatt = linkStates[pivot].tatt - linkStates.links[predecesseur].services[linkStates[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates[predecesseur].service].delta * 1440f + linkStates[pivot].h;
                                    linkStates[predecesseur].tveh = linkStates[pivot].tveh + linkStates.links[predecesseur].services[linkStates[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates[predecesseur].service].hd;
                                    linkStates[predecesseur].tcor = linkStates[pivot].tcor + temps_correspondance;
                                    linkStates[predecesseur].ncorr = linkStates[pivot].ncorr + 1;
                                    linkStates[predecesseur].l = linkStates[pivot].l + linkStates.links[predecesseur].longueur;
                                    linkStates[predecesseur].tmap = linkStates[pivot].tmap;
                                    linkStates[predecesseur].ttoll = linkStates[pivot].ttoll + linkStates.links[predecesseur].toll;

                                    //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates.links[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                    while (bucket >= gga_nq.Count)
                                    {
                                        gga_nq.Add(new List<int>());
                                    }
                                    gga_nq[bucket].Add(predecesseur);
                                    aff_hor.nb_pop++;
                                    //                                        touches.Enqueue(successeur);
                                    linkStates[predecesseur].pivot = pivot;
                                    linkStates[predecesseur].turn_pivot = j;
                                    linkStates[predecesseur].pole = projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i;
                                    linkStates[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i + linkStates[pivot].poleV2;
                                }
                            }

                            //predecesseur TC lignes différentes pivot MAP
                            else if (linkStates.links[predecesseur].ligne > 0 && linkStates.links[pivot].ligne < 0 && aff_hor.cveh[pred_type] > 0)
                            {
                                int ii, jj, num_service = -1, h3 = 0, delta, duree_periode;
                                float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                for (ii = 0; ii < linkStates.links[predecesseur].services.Count; ii++)
                                {
                                    delta = 0;
                                    duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Length;
                                    if ((linkStates.links[predecesseur].services[ii].hf >linkStates[pivot].h) || projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                                    {
                                        h1 = -1e38f;
                                        h2 = 1e38f;
                                        h3 = -1;

                                        for (jj = jour - 1; jj >= Math.Max(jour - aff_hor.nb_jours, 0); jj--)
                                        {

                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) < aff_hor[pivot].h)
                                            {
                                                h1 = linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                                h2 = (-jour + jj);
                                                h3 = jj;
                                            }

                                        }
                                        if (h3 != -1)
                                        {
                                            if (linkStates.links[predecesseur].services[ii].delta > h2 || linkStates[predecesseur].touche == 0)
                                            {
                                                linkStates.links[predecesseur].services[ii].delta = h2;
                                            }
                                        }
                                        else
                                        {
                                            delta = 1;
                                        }


                                    }
                                    if ((linkStates.links[predecesseur].services[ii].hf + linkStates.links[predecesseur].services[ii].delta * 1440f <= linkStates[pivot].h))

                                    {
                                        if ((linkStates[pivot].cout + (linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + linkStates[pivot].h) * aff_hor.cwait[pred_type]) + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type] < cout2 && delta < 1)
                                        {
                                            cout2 = linkStates[pivot].cout + (linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                            num_service = ii;

                                        }
                                    }

                                }
                                if (num_service != -1)
                                {
                                    linkStates[predecesseur].service = num_service;
                                    linkStates[predecesseur].cout = linkStates[pivot].cout + (linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].delta * 1440f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];

                                    linkStates[predecesseur].touche = 1;

                                    linkStates[predecesseur].h = linkStates[predecesseur].services[linkStates[predecesseur].service].hd + linkStates[predecesseur].services[linkStates[predecesseur].service].delta * 24f * 60f;
                                    if (linkStates[pivot].ncorr == 0)
                                    {
                                        linkStates[predecesseur].tatt1 = -linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 1440f + linkStates[pivot].h;
                                    }
                                    else
                                    {
                                        linkStates[predecesseur].tatt1 = linkStates[pivot].tatt1;

                                    }
                                    linkStates[predecesseur].tatt = linkStates[pivot].tatt - linkStates.links[predecesseur].services[linkStates[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates[predecesseur].service].delta * 1440f + linkStates[pivot].h;
                                    linkStates[predecesseur].tveh = linkStates[pivot].tveh + linkStates.links[predecesseur].services[linkStates[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates[predecesseur].service].hd;
                                    linkStates[predecesseur].tcor = linkStates[pivot].tcor;
                                    linkStates[predecesseur].ncorr = linkStates[pivot].ncorr;
                                    linkStates[predecesseur].tmap = linkStates[pivot].tmap;
                                    linkStates[predecesseur].ttoll = linkStates[pivot].ttoll + linkStates.links[predecesseur].toll;

                                    linkStates[predecesseur].l = linkStates[pivot].l + linkStates.links[predecesseur].longueur;                                                //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra,aff_hor.pu), aff_hor.max_nb_buckets)));
                                    while (bucket >= gga_nq.Count)
                                    {
                                        gga_nq.Add(new List<int>());
                                    }
                                    gga_nq[bucket].Add(predecesseur);
                                    aff_hor.nb_pop++;
                                    //                                        touches.Enqueue(successeur);
                                    linkStates[predecesseur].pivot = pivot;
                                    linkStates[predecesseur].turn_pivot = j;
                                    linkStates[predecesseur].pole = linkStates[pivot].pole;
                                    linkStates[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i + linkStates[pivot].poleV2;
                                }
                            }

                        }


                        //eléments déjà touchés
                        else if (linkStates[predecesseur].touche == 1 || linkStates[predecesseur].touche == 2)
                        {
                            //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));


                            //successeurs marche à pied pivot MAP

                            if (linkStates.links[predecesseur].ligne < 0 && linkStates.links[pivot].ligne < 0 && aff_hor.cmap[pred_type] > 0 && (linkStates[pivot].tmap + linkStates.links[predecesseur].temps < aff_hor.tmapmax) && linkStates[predecesseur].cout > linkStates[pivot].cout)
                            {
                                bool test_periode = false;
                                int id_service = -1;
                                if (linkStates.links[predecesseur].services.Count > 0)
                                {
                                    int decal_jour = (int)(Math.Floor((linkStates.links[pivot].h - penalite) / 1440f));
                                    for (int kk = 0; kk < linkStates.links[predecesseur].services.Count; kk++)
                                    {
                                        if (Math.Abs(decal_jour) <= aff_hor.nb_jours)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && linkStates.links[predecesseur].services[kk].hd + 1440f * decal_jour <= linkStates[pivot].h - penalite && linkStates.links[predecesseur].services[kk].hf + 1440f * decal_jour > linkStates[pivot].h - penalite)
                                            {
                                                test_periode = true;
                                                id_service = kk;
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    test_periode = true;
                                }

                                if (test_periode == true)
                                {


                                    if (linkStates[predecesseur].cout > linkStates[pivot].cout + (penalite + linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type] * aff_hor.cmap[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type])
                                    {

                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                        gga_nq[bucket].Remove(predecesseur);
                                        linkStates[predecesseur].cout = linkStates[pivot].cout + (penalite + linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type] * aff_hor.cmap[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                        linkStates[predecesseur].h = linkStates[pivot].h - (linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type] - penalite;
                                        linkStates[predecesseur].tatt = linkStates[pivot].tatt;
                                        linkStates[predecesseur].tatt1 = linkStates[pivot].tatt1;
                                        linkStates[predecesseur].tveh = linkStates[pivot].tveh;
                                        linkStates[predecesseur].tcor = linkStates[pivot].tcor;
                                        linkStates[predecesseur].ncorr = linkStates[pivot].ncorr;
                                        linkStates[predecesseur].tmap = linkStates[pivot].tmap + (penalite + linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type];
                                        linkStates[predecesseur].ttoll = linkStates[pivot].ttoll + linkStates.links[predecesseur].toll;

                                        linkStates[predecesseur].l = linkStates[pivot].l + linkStates.links[predecesseur].longueur;
                                        linkStates[predecesseur].touche = 2;
                                        linkStates[predecesseur].pivot = pivot;
                                        linkStates[predecesseur].turn_pivot = j;
                                        linkStates[predecesseur].pole = linkStates[pivot].pole;
                                        linkStates[predecesseur].poleV2 = linkStates[pivot].poleV2;
                                        linkStates[predecesseur].service = id_service;

                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                        gga_nq[bucket].Add(predecesseur);
                                        aff_hor.nb_pop++;

                                    }
                                }

                            }
                            //predecesseurs marche à pied pivot TC
                            else if (linkStates.links[predecesseur].ligne < 0 && linkStates.links[pivot].ligne > 0 && aff_hor.cmap[pred_type] > 0 && (linkStates[pivot].tmap + linkStates.links[predecesseur].temps < aff_hor.tmapmax) && linkStates[predecesseur].cout > linkStates[pivot].cout)
                            {
                                int id_service = -1;
                                bool test_periode = false;

                                if (linkStates.links[predecesseur].services.Count > 0)
                                {
                                    int decal_jour = -(int)(Math.Floor((linkStates[pivot].h - temps_correspondance) / 1440f));
                                    for (int kk = 0; kk < linkStates.links[predecesseur].services.Count; kk++)
                                    {
                                        if (decal_jour <= aff_hor.nb_jours)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[kk].regime].Substring(jour - decal_jour, 1) == "O" && linkStates.links[predecesseur].services[kk].hd - 1440f * decal_jour <= linkStates[pivot].h - penalite - temps_correspondance && linkStates.links[predecesseur].services[kk].hf - 1440f * decal_jour > linkStates[pivot].h - penalite - temps_correspondance)
                                            {
                                                test_periode = true;
                                                id_service = kk;

                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    test_periode = true;
                                }

                                if (test_periode == true)
                                {


                                    if (linkStates[predecesseur].cout > linkStates[pivot].cout + (penalite + linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type] * aff_hor.cmap[pred_type] + aff_hor.cboa[pivot_type] * temps_correspondance + aff_hor.cwait[pred_type] * temps_correspondance + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type])
                                    {
                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                        gga_nq[bucket].Remove(predecesseur);
                                        linkStates[predecesseur].cout = linkStates[pivot].cout + (penalite + linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type] * aff_hor.cmap[pred_type] + aff_hor.cboa[pivot_type] * temps_correspondance + aff_hor.cwait[pred_type] * temps_correspondance + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                        linkStates[predecesseur].h = linkStates[pivot].h - (linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type] - temps_correspondance - penalite;
                                        linkStates[predecesseur].tatt = linkStates[pivot].tatt + temps_correspondance;
                                        linkStates[predecesseur].tatt1 = linkStates[pivot].tatt1;
                                        linkStates[predecesseur].tveh = linkStates[pivot].tveh;
                                        linkStates[predecesseur].tcor = linkStates[pivot].tcor + temps_correspondance;
                                        linkStates[predecesseur].ncorr = linkStates[pivot].ncorr + 1;
                                        linkStates[predecesseur].l = linkStates[pivot].l + linkStates.links[predecesseur].longueur;
                                        linkStates[predecesseur].tmap = linkStates[pivot].tmap + (penalite + linkStates.links[predecesseur].temps) * aff_hor.coef_tmap[pred_type];
                                        linkStates[predecesseur].ttoll = linkStates[pivot].ttoll + linkStates.links[predecesseur].toll;
                                        linkStates[predecesseur].touche = 2;

                                        linkStates[predecesseur].pivot = pivot;
                                        linkStates[predecesseur].pole = projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i;
                                        linkStates[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i + linkStates[pivot].poleV2;

                                        linkStates[predecesseur].turn_pivot = j;

                                        linkStates[predecesseur].service = id_service;
                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                        gga_nq[bucket].Add(predecesseur);
                                        aff_hor.nb_pop++;

                                    }
                                }

                            }
                            //successeurs TC même ligne
                            else if ((linkStates.links[predecesseur].ligne == linkStates.links[pivot].ligne && aff_hor.cveh[pred_type] > 0 && linkStates.links[pivot].ligne > 0) && (linkStates[predecesseur].cout > linkStates[pivot].cout))
                            {
                                int ii, num_service = -1;
                                for (ii = 0; ii < linkStates.links[predecesseur].services.Count; ii++)
                                {
                                    if (linkStates.links[predecesseur].services[ii].numero == linkStates.links[pivot].services[linkStates.links[pivot].service].numero)
                                    {
                                        if (linkStates.links[predecesseur].services[ii].hf <= linkStates.links[pivot].services[linkStates.links[pivot].service].hd)
                                        {
                                            num_service = ii;
                                        }
                                    }


                                }

                                if (num_service != -1)
                                {
                                    if (linkStates.links[predecesseur].services[num_service].hf + linkStates.links[pivot].services[linkStates.[pivot].service].delta * 1440f <= linkStates[pivot].h)
                                    {

                                        if (linkStates[predecesseur].cout > linkStates[pivot].cout + (-linkStates.links[predecesseur].services[num_service].hd - linkStates.links[pivot].services[linkStates[pivot].service].delta * 1440f + linkStates[pivot].h) * aff_hor.cveh[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type] && (linkStates.links[predecesseur].services[linkStates[predecesseur].service].hf <= linkStates.links[pivot].services[linkStates[pivot].service].hd))
                                        {
                                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                            gga_nq[bucket].Remove(predecesseur);
                                            linkStates[predecesseur].service = num_service;
                                            linkStates.links[predecesseur].services[num_service].delta = linkStates.links[pivot].services[linkStates.links[pivot].service].delta;

                                            linkStates[predecesseur].touche = 2;
                                            linkStates[predecesseur].cout = linkStates[pivot].cout + (-linkStates.links[predecesseur].services[linkStates[predecesseur].service].hd - linkStates.links[predecesseur].services[linkStates[predecesseur].service].delta * 1440f + linkStates[pivot].h) * aff_hor.cveh[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                            linkStates[predecesseur].h = linkStates.links[predecesseur].services[linkStates[predecesseur].service].hd + linkStates.links[predecesseur].services[linkStates[predecesseur].service].delta * 60f * 24f;
                                            linkStates[predecesseur].tatt = linkStates[pivot].tatt;
                                            linkStates[predecesseur].tatt1 = linkStates[pivot].tatt1;
                                            linkStates[predecesseur].tveh = linkStates[pivot].tveh - linkStates.links[predecesseur].services[linkStates[predecesseur].service].hd /*- linkStates.links[predecesseur].services[linkStates[predecesseur].service].delta * 1440f*/ + linkStates[pivot].h;
                                            linkStates[predecesseur].tcor = linkStates[pivot].tcor;
                                            linkStates[predecesseur].ncorr = linkStates[pivot].ncorr;
                                            linkStates[predecesseur].l = linkStates[pivot].l + linkStates.links[predecesseur].longueur;
                                            linkStates[predecesseur].tmap = linkStates[pivot].tmap;
                                            linkStates[predecesseur].ttoll = linkStates[pivot].ttoll + linkStates.links[predecesseur].toll;

                                            linkStates.links[predecesseur].pivot = pivot;
                                            linkStates.links[predecesseur].turn_pivot = j;
                                            linkStates.links[predecesseur].pole = linkStates.links[pivot].pole;
                                            linkStates.links[predecesseur].poleV2 = linkStates.links[pivot].poleV2;

                                            //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                            bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                            gga_nq[bucket].Add(predecesseur);
                                            aff_hor.nb_pop++;
                                        }
                                    }
                                }
                            }
                            //successeurs TC lignes différentes
                            else if ((linkStates.links[predecesseur].ligne != linkStates.links[pivot].ligne && aff_hor.cveh[pred_type] > 0 && linkStates.links[pivot].ligne > 0 && linkStates.links[predecesseur].ligne > 0) && (linkStates[predecesseur].cout > linkStates[pivot].cout))
                            {
                                int ii, jj, num_service = -1, h3 = -1, delta, duree_periode;
                                float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                for (ii = 0; ii < linkStates.links[predecesseur].services.Count; ii++)
                                {
                                    delta = 0;
                                    duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Length;

                                    if (linkStates.links[predecesseur].services[ii].hf + temps_correspondance > linkStates[pivot].h || projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                                    {

                                        h1 = -1e38f;
                                        h2 = 1e38f;
                                        h3 = -1;
                                        for (jj = jour - 1; jj >= Math.Max(jour - aff_hor.nb_jours, 0); jj--)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) + temps_correspondance < linkStates[pivot].h)
                                            {
                                                h1 = linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                                h2 = (-jour + jj);
                                                h3 = jj;
                                            }

                                        }
                                        if (h3 != -1)
                                        {
                                            if (linkStates.links[predecesseur].services[ii].delta > h2 || linkStates.links[predecesseur].touche == 0)
                                            {
                                                linkStates.links[predecesseur].services[ii].delta = h2;
                                            }

                                        }
                                        else
                                        {
                                            delta = 1;
                                        }


                                    }
                                    if ((linkStates.links[predecesseur].services[ii].hf + linkStates.links[predecesseur].services[ii].delta * 1440f + max_correspondance >= linkStates[pivot].h) && (linkStates.links[predecesseur].services[ii].hf + linkStates.links[predecesseur].services[ii].delta * 1440f + temps_correspondance <= linkStates[pivot].h))

                                    {
                                        if (linkStates[pivot].cout + (linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + (temps_correspondance * aff_hor.cboa[pivot_type]) + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type] < cout2 && delta < 1)
                                        {
                                            cout2 = linkStates[pivot].cout + (linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + (temps_correspondance * aff_hor.cboa[pivot_type]) + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                            num_service = ii;
                                        }
                                    }

                                }

                                if (num_service != -1)
                                {
                                    if (linkStates[predecesseur].cout > linkStates[pivot].cout + (linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].delta * 1440f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + (temps_correspondance * aff_hor.cboa[pivot_type]) + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type])
                                    {
                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra,aff_hor.pu), aff_hor.max_nb_buckets)));
                                        gga_nq[bucket].Remove(predecesseur);
                                        linkStates[predecesseur].service = num_service;
                                        linkStates[predecesseur].cout = linkStates[pivot].cout + (linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].delta * 1440f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + (temps_correspondance * aff_hor.cboa[pivot_type]) + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                        linkStates[predecesseur].touche = 2;

                                        linkStates[predecesseur].h = linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hd + linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 60f * 24f;
                                        if (linkStates[pivot].tatt1 == 0)
                                        {
                                            linkStates[predecesseur].tatt1 = -linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 1440f + linkStates[pivot].h;
                                        }
                                        else
                                        {
                                            linkStates[predecesseur].tatt1 = linkStates[pivot].tatt1;

                                        }
                                        linkStates[predecesseur].tatt = linkStates[pivot].tatt - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 1440f + linkStates[pivot].h;
                                        linkStates[predecesseur].tveh = linkStates[pivot].tveh + linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hd;
                                        linkStates[predecesseur].tcor = linkStates[pivot].tcor + temps_correspondance;
                                        linkStates[predecesseur].ncorr = linkStates[pivot].ncorr + 1;
                                        linkStates[predecesseur].l = linkStates[pivot].l + linkStates.links[predecesseur].longueur;
                                        linkStates[predecesseur].tmap = linkStates[pivot].tmap;
                                        linkStates[predecesseur].ttoll = linkStates[pivot].ttoll + linkStates.links[predecesseur].toll;

                                        linkStates[predecesseur].pivot = pivot;
                                        linkStates[predecesseur].turn_pivot = j;
                                        linkStates[predecesseur].pole = projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i;
                                        linkStates[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i + "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i + linkStates[pivot].poleV2;

                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout /aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                        gga_nq[bucket].Add(predecesseur);
                                        aff_hor.nb_pop++;
                                    }
                                }

                            }
                            //predecesseurs TC lignes différentes pivot MAP
                            else if ((linkStates.links[predecesseur].ligne > 0 && linkStates.links[predecesseur].ligne != linkStates.links[pivot].ligne && aff_hor.cveh[pred_type] > 0 && linkStates.links[pivot].ligne < 0) && (linkStates[predecesseur].cout > linkStates[pivot].cout))
                            {
                                int ii, jj, num_service = -1, h3 = -1, delta, duree_periode;
                                float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                for (ii = 0; ii < linkStates.links[predecesseur].services.Count; ii++)
                                {
                                    delta = 0;

                                    duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Length;

                                    if (linkStates.links[predecesseur].services[ii].hf + temps_correspondance >linkStates[pivot].h || projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                                    {

                                        h1 = -1e38f;
                                        h2 = 1e38f;
                                        h3 = -1;
                                        for (jj = jour - 1; jj >= Math.Max(jour - aff_hor.nb_jours, 0); jj--)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].nom_calendrier[linkStates.links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) < linkStates[pivot].h)
                                            {
                                                h1 = linkStates.links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                                h2 = (-jour + jj);
                                                h3 = jj;
                                            }

                                        }
                                        if (h3 != -1)
                                        {
                                            if (linkStates.links[predecesseur].services[ii].delta > h2 || linkStates.links[predecesseur].touche == 0)
                                            {
                                                linkStates.links[predecesseur].services[ii].delta = h2;
                                            }
                                        }
                                        else
                                        {
                                            delta = 1;
                                        }


                                    }
                                    if ((linkStates.links[predecesseur].services[ii].hf + linkStates.links[predecesseur].services[ii].delta * 1440f + max_correspondance >= linkStates[pivot].h) && (linkStates.links[predecesseur].services[ii].hf + linkStates.links[predecesseur].services[ii].delta * 1440f + temps_correspondance <= linkStates[pivot].h))

                                    {
                                        if (linkStates[pivot].cout + (linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type] < cout2 && delta < 1)
                                        {
                                            num_service = ii;
                                            cout2 = linkStates[pivot].cout + (linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[ii].hf - linkStates.links[predecesseur].services[ii].delta * 60f * 24f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                        }
                                    }
                                }

                                if (num_service != -1)
                                {
                                    if (linkStates[predecesseur].cout > linkStates[pivot].cout + (linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].delta * 1440f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type])
                                    {

                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                        gga_nq[bucket].Remove(predecesseur);
                                        linkStates[predecesseur].service = num_service;
                                        linkStates[predecesseur].cout = linkStates[pivot].cout + (linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].hd) * aff_hor.cveh[pred_type] + (-linkStates.links[predecesseur].services[num_service].hf - linkStates.links[predecesseur].services[num_service].delta * 1440f + linkStates[pivot].h) * aff_hor.cwait[pred_type] + linkStates.links[predecesseur].toll * aff_hor.ctoll[pred_type];
                                        linkStates[predecesseur].touche = 2;

                                        linkStates[predecesseur].h = linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hd + linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 60f * 24f;
                                        if (linkStates.links[pivot].tatt1 == 0)
                                        {
                                            linkStates.links[predecesseur].tatt1 = -linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 1440f + linkStates.links[pivot].h;
                                        }
                                        else
                                        {
                                            linkStates.links[predecesseur].tatt1 = linkStates.links[pivot].tatt1;

                                        }
                                        linkStates.links[predecesseur].tatt = linkStates.links[pivot].tatt - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].delta * 1440f + linkStates.links[pivot].h;
                                        linkStates.links[predecesseur].tveh = linkStates.links[pivot].tveh + linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hf - linkStates.links[predecesseur].services[linkStates.links[predecesseur].service].hd;
                                        linkStates.links[predecesseur].tcor = linkStates.links[pivot].tcor;
                                        linkStates.links[predecesseur].ncorr = linkStates.links[pivot].ncorr;
                                        linkStates.links[predecesseur].tmap = linkStates.links[pivot].tmap;
                                        linkStates.links[predecesseur].ttoll = linkStates.links[pivot].ttoll + linkStates.links[predecesseur].toll;

                                        linkStates.links[predecesseur].l = linkStates.links[pivot].l + linkStates.links[predecesseur].longueur;
                                        linkStates.links[predecesseur].pivot = pivot;
                                        linkStates.links[predecesseur].turn_pivot = j;
                                        linkStates.links[predecesseur].pole = linkStates.links[pivot].pole;
                                        linkStates.links[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i + linkStates.links[pivot].poleV2;
                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(linkStates.links[predecesseur].cout, 2) / aff_hor.param_dijkstra), aff_hor.max_nb_buckets));
                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(linkStates.links[predecesseur].cout / aff_hor.param_dijkstra, aff_hor.pu), aff_hor.max_nb_buckets)));
                                        gga_nq[bucket].Add(predecesseur);
                                        aff_hor.nb_pop++;
                                    }
                                }
                            }


                        }

                        /*List<int> liste = new List<int>();
                        liste.Add(1289714);
                        liste.Add(233502);
                        liste.Add(233499);
                        liste.Add(1289712);

                        if (liste.Contains(pivot) || liste.Contains(predecesseur))
                            {
                            String texto = "pivot:" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[pivot].no].i.ToString() + " " +
                               projet.reseaux[projet.reseau_actif].nodes[linkStates.links[pivot].nd].i.ToString() + " " + 
                                linkStates.links[pivot].h.ToString() +
                                " " + linkStates.links[pivot].cout.ToString() +
                                " " + linkStates.links[pivot].tmap.ToString()+
                                " " + linkStates.links[pivot].tveh.ToString()  +
                                " " + linkStates.links[pivot].tatt.ToString() + '\n' +
                                "pred:" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].no].i.ToString() + " " +
                               projet.reseaux[projet.reseau_actif].nodes[linkStates.links[predecesseur].nd].i.ToString() + 
                                " " + linkStates.links[predecesseur].h.ToString() +
                                " " + linkStates.links[predecesseur].cout.ToString() +
                                " " + linkStates.links[predecesseur].tmap.ToString()+
                                " " + linkStates.links[predecesseur].tveh.ToString() +
                                " " + linkStates.links[predecesseur].tatt.ToString() +'\n'; 
                            fich_log.Write(texto);
                        }*/

                    }
                }
                //linkStates.links[pivot].touche = 3;
                //Console.WriteLine((touches.Count+calcules.Count).ToString());
            }
        fin_gga2:
            //Console.WriteLine(p.ToString());

            int arrivee = -1;
            double cout_fin = 1e38f;
            if (linkStates.numnoeud.TryGetValue(p, out value) == true)
            {
                for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[linkStates.numnoeud[p]].succ.Count; j++)
                {
                    predecesseur = projet.reseaux[projet.reseau_actif].nodes[linkStates.numnoeud[p]].succ[j];
                    if (linkStates.links[predecesseur].touche != 0 && linkStates.links[predecesseur].cout < cout_fin)
                    {
                        arrivee = predecesseur;
                        cout_fin = linkStates.links[predecesseur].cout;

                    }




                }
            }
            else
            {
                fich_log.WriteLine("OD error" + libod + ":" + ":" + chaine + ": non existing origin node!");
            }

            if (arrivee != -1)
            {
                if (linkStates.links[arrivee].ligne > 0)
                {
                    linkStates.links[arrivee].boai += od;
                    linkStates.links[arrivee].services[linkStates.links[arrivee].service].boai = od;
                    linkStates.links[arrivee].services[linkStates.links[arrivee].service].boat += od;
                }
            }
            else
            {
                fich_log.WriteLine("OD error" + libod + ":" + chaine + ": unreachable origin node!");
            }

            pivot = arrivee;
            string itineraire = "", texte;
            if (pivot != -1)
            {
                string[] param2 = { "|" }, lignes_corr = null;
                if (linkStates.links[pivot].texte != null)
                {

                    lignes_corr = linkStates.links[pivot].texte.Split(param2, StringSplitOptions.RemoveEmptyEntries);
                }
                if (lignes_corr == null)
                {
                    itineraire = "MAP";
                }
                else
                {
                    itineraire = lignes_corr[0];
                }
            }
            while (pivot != -1)
            {
                linkStates.links[pivot].volau += od;
                if (linkStates.links[pivot].pivot != -1 && aff_hor.sortie_turns == true)
                {
                    Turn virage = new Turn();
                    virage.arci = pivot;
                    virage.arcj = linkStates.links[pivot].pivot;
                    float value2;
                    if (transfers.TryGetValue(virage, out value2) == true)
                    {

                        transfers[virage] += od;
                    }
                    else
                    {
                        transfers[virage] = od;
                    }

                    //linkStates.links[linkStates.links[pivot].pivot].arci[linkStates.links[pivot].turn_pivot].volau += od;
                }
                if (linkStates.links[pivot].service >= 0)
                {
                    linkStates.links[pivot].services[linkStates.links[pivot].service].volau += od;
                }

                if (linkStates.links[pivot].pivot == -1)
                {
                    if (linkStates.links[pivot].ligne > 0)
                    {
                        linkStates.links[pivot].alij += od;
                        linkStates.links[pivot].services[linkStates.links[pivot].service].alij = od;
                        linkStates.links[pivot].services[linkStates.links[pivot].service].alit += od;

                    }
                }
                else if (linkStates.links[pivot].ligne != linkStates.links[linkStates.links[pivot].pivot].ligne)
                {
                    if (linkStates.links[pivot].ligne > 0)
                    {
                        linkStates.links[pivot].alij += od;
                        linkStates.links[pivot].services[linkStates.links[pivot].service].alij = od;
                        linkStates.links[pivot].services[linkStates.links[pivot].service].alit += od;

                    }
                    if (linkStates.links[linkStates.links[pivot].pivot].ligne > 0)
                    {
                        linkStates.links[linkStates.links[pivot].pivot].boai += od;
                        linkStates.links[linkStates.links[pivot].pivot].services[linkStates.links[linkStates.links[pivot].pivot].service].boai = od;
                        linkStates.links[linkStates.links[pivot].pivot].services[linkStates.links[linkStates.links[pivot].pivot].service].boat += od;
                    }

                }
                if (aff_hor.sortie_chemins == true)
                {
                    texte = libod + ";" + p + ";" + q + ";" + jour.ToString("0") + ";" + horaire.ToString("0.000");
                    texte += ";" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[pivot].no].i;
                    texte += ";" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[pivot].nd].i;
                    texte += ";" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[pivot].no].i + "-" + projet.reseaux[projet.reseau_actif].nodes[linkStates.links[pivot].nd].i;

                    texte += ";" + linkStates.links[pivot].ligne.ToString("0");
                    if (linkStates.links[pivot].service >= 0)
                    {
                        texte += ";" + linkStates.links[pivot].services[linkStates.links[pivot].service].numero.ToString("0");
                    }
                    else
                    {
                        texte += ";-1";
                    }
                    texte += ";" + (-linkStates.links[pivot].h + horaire).ToString("0.000");
                    texte += ";" + linkStates.links[pivot].h.ToString("0.000");
                    texte += ";" + linkStates.links[pivot].tveh.ToString("0.000");
                    texte += ";" + linkStates.links[pivot].tmap.ToString("0.000");
                    texte += ";" + linkStates.links[pivot].tatt.ToString("0.000");
                    texte += ";" + linkStates.links[pivot].tcor.ToString("0.000");
                    texte += ";" + linkStates.links[pivot].ncorr.ToString("0.000");
                    texte += ";" + linkStates.links[pivot].tatt1.ToString("0.000");
                    texte += ";" + linkStates.links[pivot].cout.ToString("0.000");
                    texte += ";" + linkStates.links[pivot].l.ToString("0.000");
                    texte += ";" + linkStates.links[pivot].pole;
                    texte += ";" + od.ToString("0.00");
                    if (linkStates.links[pivot].ligne != -1)
                    {
                        texte += ";" + linkStates.links[pivot].services[linkStates.links[pivot].service].boai.ToString("0.000");
                        texte += ";" + linkStates.links[pivot].services[linkStates.links[pivot].service].alij.ToString("0.000");
                        linkStates.links[pivot].services[linkStates.links[pivot].service].boai = 0;
                        linkStates.links[pivot].services[linkStates.links[pivot].service].alij = 0;

                    }
                    else
                    {
                        texte += ";0";
                        texte += ";0";
                    }
                    texte += ";" + linkStates.links[pivot].texte;
                    texte += ";" + linkStates.links[pivot].type;

                    texte += ";" + linkStates.links[pivot].ttoll.ToString("0.000");


                    fich_sortie2.WriteLine(texte);



                }
                if (linkStates.links[pivot].pivot != -1)
                {
                    if (linkStates.links[pivot].ligne != linkStates.links[linkStates.links[pivot].pivot].ligne)
                    {
                        string[] param2 = { "|" }, lignes_corr = null;
                        if (linkStates.links[pivot].texte != null)
                        {


                            lignes_corr = linkStates.links[linkStates.links[pivot].pivot].texte.Split(param2, StringSplitOptions.RemoveEmptyEntries);
                        }
                        if (lignes_corr == null)
                        {
                            itineraire = itineraire + "|MAP"; ;
                        }
                        else
                        {
                            itineraire = itineraire + "|" + lignes_corr[0];
                        }
                    }
                }
                pivot = linkStates.links[pivot].pivot;


            }

        // Classes inchangées
        public class vecteur { public List<float> d = new List<float>(0); }
        public class Turn
        {
            public int arci, arcj;
            public override bool Equals(Object virage)
            {
                return arci == ((Turn)virage).arci && arcj == ((Turn)virage).arcj;
            }
            public override int GetHashCode()
            {
                return arci.GetHashCode() ^ arcj.GetHashCode();
            }
        }
        public class turn { public int numero; public float temps; public bool is_valid = false; }
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
                return i == ((Link_num)num_link).i && j == ((Link_num)num_link).j && line == ((Link_num)num_link).line;
            }
            public override int GetHashCode()
            {
                return (i.GetHashCode() ^ j.GetHashCode() ^ line.GetHashCode());
            }
        }
        public class link
        {
            public float longueur, temps, cout, /*v0, vsat,*/ tatt, tcor, tveh, tmap, tatt1, /*a, b, n, lanes,*/ volau, h, l, alij, boai, ncorr, toll = 0, ttoll;
            public int no, nd, service, vdf, touche, pivot, ligne, turn_pivot = -1;
            public bool is_queue, is_valid = true;
            public List<Service> services = new List<Service>();
            public string texte, modes, pole, type = "0", poleV2;
            public link Clone()
            {
                return new link
                {
                    longueur = this.longueur,
                    temps = this.temps,
                    cout = this.cout,
                    tatt = this.tatt,
                    tcor = this.tcor,
                    tveh = this.tveh,
                    tmap = this.tmap,
                    tatt1 = this.tatt1,
                    volau = this.volau,
                    h = this.h,
                    l = this.l,
                    alij = this.alij,
                    boai = this.boai,
                    ncorr = this.ncorr,
                    toll = this.toll,
                    ttoll = this.ttoll,

                    no = this.no,
                    nd = this.nd,
                    service = this.service,
                    vdf = this.vdf,
                    touche = this.touche,
                    pivot = this.pivot,
                    ligne = this.ligne,
                    turn_pivot = this.turn_pivot,

                    is_queue = this.is_queue,
                    is_valid = this.is_valid,

                    texte = this.texte,
                    modes = this.modes,
                    pole = this.pole,
                    type = this.type,
                    poleV2 = this.poleV2,

                    // 🔥 IMPORTANT : clone de la liste + contenu
                    services = this.services
                        .Select(s => s.Clone())
                        .ToList()
                };
            }

        }
        public class matrix { public string nom; public List<vecteur> o = new List<vecteur>(0); }
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
            public string texte_coef_tmap, texte_cmap, texte_cwait, texte_cboa, texte_tboa, texte_tboa_max, texte_cveh, texte_toll;
            public float tmapmax, temps_max = 120;
            public int nb_pop = 0;
            public string texte_filtre_sortie = "";
        }
        public class Service
        {
            public int numero;
            public float hd, hf, delta = 0, boai = 0, alij = 0, alit = 0, boat = 0, volau = 0;
            public int regime;
            public Service Clone()
            {
                return new Service
                {
                    numero = this.numero,
                    hd = this.hd,
                    hf = this.hf,
                    delta = this.delta,
                    boai = this.boai,
                    alij = this.alij,
                    alit = this.alit,
                    boat = this.boat,
                    volau = this.volau,
                    regime = this.regime
                };
            }
        }
        public class suivant { public List<int> classe = new List<int>(); }
        public class Node
        {
            public string i = "";
            public float x = 0, y = 0, tempst = 1e38f, tmap = 0, tatt, temps, cout, ncor, ttoll;
            public string name = "";
            public string pole, poleV2;
            public List<int> in_nodes = new List<int>();
            public List<int> out_nodes = new List<int>();
            public void addincoming(int i) { this.in_nodes.Add(i); }
            public void addoutgoing(int i) { this.out_nodes.Add(i); }
        }
        public class Link { }
    }
}
