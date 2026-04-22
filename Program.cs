// ============================================================
//  MUSLIC — Version parallélisée
//  Algorithme original préservé intégralement.
//  Parallélisation par groupe d'origines (même p, jour, horaire, sens).
//  Chaque thread a son propre LinkState[] (champs mutables Dijkstra).
//  Accumulation volau/boai/alij séquentielle après le calcul.
// ============================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Muslic
{
    class Program
    {
        // ── État Dijkstra par lien, local à chaque thread ──────────────
        public class LinkState
        {
            public float cout, h, tatt, tatt1, tcor, tmap, tveh, ttoll, l, ncorr;
            public float boai, alij, volau;
            public int touche, pivot, turn_pivot, service;
            public bool is_queue;
            public string pole = "-1", poleV2 = "";
        }

        // ── Résultat d'une paire OD (lignes de sortie + accumulations) ─
        public class LignesOD
        {
            public string od_line;
            public List<string> chemins = new List<string>();
            public List<string> temps = new List<string>();
            public List<string> noeuds = new List<string>();
            public string detour_line;
            public List<string> isoles = new List<string>();
            public List<(int idx, float volau, int svc, float boai, float boat, float alij, float alit)> accum
                = new List<(int, float, int, float, float, float, float)>();
        }

        // ── Accumulation temporaire par service (locale au thread) ─────
        public class SvcAccum
        {
            public float volau, boai, boat, alij, alit;
        }

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
        }

        public static void Ecrit_parametres(Param_affectation_horaire parametres, string nom_fichier_ini)
        {

            System.IO.StreamWriter fich_ini = new System.IO.StreamWriter(nom_fichier_ini, false, System.Text.Encoding.UTF8);
            if (parametres.sortie_stops == true)
            {
                parametres.sortie_temps += 10;
            }
            ;
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
                //                Console.WriteLine(aff_hor.algorithme);
                aff_hor.demitours = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.demitours);
                aff_hor.max_nb_buckets = int.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.max_nb_buckets);
                aff_hor.nb_jours = int.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.nb_jours);
                aff_hor.nom_matrice = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.nom_matrice);
                aff_hor.nom_penalites = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.nom_penalites);
                aff_hor.nom_reseau = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.nom_reseau);
                aff_hor.nom_sortie = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.nom_sortie);
                aff_hor.param_dijkstra = int.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.param_dijkstra);
                aff_hor.pu = float.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.pu);
                aff_hor.sortie_chemins = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.sortie_chemins);
                aff_hor.sortie_services = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.sortie_services);
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
                //Console.WriteLine(aff_hor.sortie_temps);
                aff_hor.sortie_turns = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.sortie_turns);
                aff_hor.texte_cboa = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.texte_cboa);
                aff_hor.texte_cmap = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.texte_cmap);
                aff_hor.texte_coef_tmap = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.texte_coef_tmap);
                aff_hor.texte_cveh = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.texte_cveh);
                aff_hor.texte_cwait = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.texte_cwait);
                aff_hor.texte_tboa = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.texte_tboa);
                aff_hor.texte_tboa_max = fich_ini.ReadLine().Split(';')[0];
                //Console.WriteLine(aff_hor.texte_tboa_max);
                //aff_hor.sortie_noeuds = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.texte_tboa_max);
                // aff_hor.sortie_isoles = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                //Console.WriteLine(aff_hor.texte_tboa_max);

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

                    //  Console.WriteLine(aff_hor.tmapmax);
                }
                if (fich_ini.EndOfStream == false)
                {
                    aff_hor.texte_toll = fich_ini.ReadLine().Split(';')[0];
                    //Console.WriteLine(aff_hor.texte_toll);
                }

                if (fich_ini.EndOfStream == false)
                {
                    aff_hor.texte_filtre_sortie = fich_ini.ReadLine().Split(';')[0];
                    //Console.WriteLine(aff_hor.texte_filtre_sortie);
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
                    //Console.WriteLine(aff_hor.temps_max);
                }
                if (fich_ini.EndOfStream == false)
                {
                    aff_hor.sortie_noeuds = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                    //Console.WriteLine(aff_hor.sortie_noeuds);
                }
                if (fich_ini.EndOfStream == false)
                {
                    aff_hor.sortie_isoles = bool.Parse(fich_ini.ReadLine().Split(';')[0]);
                    //Console.WriteLine(aff_hor.sortie_isoles);
                }
                fich_ini.Close();
            }
            aff_hor.test_OK = true;
            return aff_hor;
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
            //string nom_reseau = aff_hor.nom_reseau;
            //string nom_matrice = aff_hor.nom_matrice;
            //string nom_penalites = aff_hor.nom_penalites;
            //Console.WriteLine(nom_sortie);
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
                    //openFileDialog1.ShowDialog();

                    string carte = "t links";

                    int avancement = 0;
                    int ctop = Console.CursorTop;
                    int cleft = Console.CursorLeft;
                    Console.SetCursorPosition(cleft, ctop);
                    Console.Write("Network import:" + avancement + "%");
                    //            System.Globalization.NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator = ".";
                    System.IO.FileStream flux_reseau;

                    flux_reseau = new System.IO.FileStream(nom_reseau, System.IO.FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                    System.IO.StreamReader fichier_reseau = new System.IO.StreamReader(flux_reseau, Encoding.UTF8);
                    //   projet.reseaux[num_res].matrices.Add(new matrix());
                    System.IO.StreamWriter fich_log = new System.IO.StreamWriter(aff_hor.nom_sortie + "_log.txt", false, System.Text.Encoding.UTF8);
                    fich_log.WriteLine("Version: Muslic " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    fich_log.WriteLine("Process start time: " + System.DateTime.Now.ToString("dddd dd MMMM yyyy HH:mm:ss.fff"));
                    fich_log.WriteLine("Default parameters:");
                    fich_log.WriteLine("Minimum transfer time:" + aff_hor.texte_tboa);
                    fich_log.WriteLine("Maximum transfer time:" + aff_hor.texte_tboa_max);
                    fich_log.WriteLine("Transfer weight:" + aff_hor.texte_cboa);
                    fich_log.WriteLine("Wait weight:" + aff_hor.texte_cwait);
                    fich_log.WriteLine("Time based links weight:" + aff_hor.texte_cveh);
                    fich_log.WriteLine("Individual links weight:" + aff_hor.texte_cmap);
                    fich_log.WriteLine("Individual travel time factor:" + aff_hor.texte_coef_tmap);
                    fich_log.WriteLine("Generalized travel time maximum:" + aff_hor.temps_max);
                    fich_log.WriteLine("Individual travel time maximum:" + aff_hor.tmapmax.ToString());
                    fich_log.WriteLine("Toll weight:" + aff_hor.texte_toll.ToString());
                    fich_log.WriteLine("Number of days:" + aff_hor.nb_jours);
                    fich_log.WriteLine("Prohibited U-turns:" + aff_hor.demitours);

                    fich_log.WriteLine("Algorithm:" + aff_hor.algorithme);
                    fich_log.WriteLine("Algorithm number of buckets:" + aff_hor.max_nb_buckets);
                    fich_log.WriteLine("Algorithm scale parameter:" + aff_hor.param_dijkstra);
                    fich_log.WriteLine("Algorithm power parameter:" + aff_hor.pu);

                    fich_log.WriteLine("Output paths:" + aff_hor.sortie_chemins);
                    fich_log.WriteLine("Output times:" + aff_hor.sortie_temps);
                    fich_log.WriteLine("Output filenames:" + aff_hor.nom_sortie);
                    fich_log.WriteLine("Link type filter:" + aff_hor.texte_filtre_sortie.ToString());
                    fich_log.WriteLine("Output nodes:" + aff_hor.sortie_noeuds.ToString());
                    fich_log.WriteLine("Output isolated nodes:" + aff_hor.sortie_isoles.ToString());



                    projet.reseaux[projet.reseau_actif].nom = System.IO.Path.GetFileNameWithoutExtension(nom_reseau);
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
                        //MessageBox.Show(carte + " " + ch[0]);
                        //if ((Convert.ToSingle(ch[4]) > projet.param_affectation_horaire.deb_per && Convert.ToSingle(ch[4]) < projet.param_affectation_horaire.fin_per) || Convert.ToSingle(ch[4])<0)
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
                                {
                                    projet.reseaux[num_res].xu = xi;
                                }
                                if (xi < projet.reseaux[num_res].xl)
                                {
                                    projet.reseaux[num_res].xl = xi;
                                }
                                if (yi > projet.reseaux[num_res].yu)
                                {

                                    projet.reseaux[num_res].yu = yi;

                                }
                                if (yi < projet.reseaux[num_res].yl)
                                {
                                    projet.reseaux[num_res].yl = yi;
                                }


                                if (ch.Length > 3)
                                {
                                    noeud.texte = ch[3];
                                }


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
                            /*while (projet.reseaux[projet.reseau_actif].nodes.Count < ni + 1)
                            {
                                projet.reseaux[projet.reseau_actif].nodes.Add(nul);
                            }
                            //projet.reseaux[projet.reseau_actif].numnoeud.Add(ni, projet.reseaux[projet.reseau_actif].nodes.Count);*/
                            int value;
                            if (projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(ni, out value) == false)
                            {
                                projet.reseaux[projet.reseau_actif].numnoeud.Add(ni, projet.reseaux[projet.reseau_actif].nodes.Count);
                                projet.reseaux[projet.reseau_actif].nodes.Add(nodei);
                            }
                            /*if (projet.reseaux[projet.reseau_actif].nodes[ni].i == 0)
                            {
                                projet.reseaux[projet.reseau_actif].nodes[ni] = nodei;
                            }*/

                            string nj = ch[1].Trim();

                            nodej.i = nj;
                            /*while (projet.reseaux[projet.reseau_actif].nodes.Count < nj + 1)
                            {
                                projet.reseaux[projet.reseau_actif].nodes.Add(nul);
                            }
                            if (projet.reseaux[projet.reseau_actif].nodes[nj].i == 0)
                            {
                                projet.reseaux[projet.reseau_actif].nodes[nj] = nodej;
                            }*/
                            //  MessageBox.Show(projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(nj, out value).ToString()+" "+nj.ToString()+" "+value.ToString());
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
                            if (num_service.hd < 100f && num_service.numero >= 0)
                            {
                                // num_service.hd += 1440f;
                            }
                            if (num_service.hf < 100f && num_service.numero >= 0)
                            {
                                //num_service.hf += 1440f;
                            }
                            if (num_service.hf < num_service.hd)
                            {
                                num_service.hf += 1440f;
                            }

                            if (projet.reseaux[projet.reseau_actif].num_calendrier.TryGetValue(ch[8].ToString().Trim(), out value) == false)
                            {
                                projet.reseaux[projet.reseau_actif].num_calendrier.Add(ch[8].ToString().Trim(), projet.reseaux[projet.reseau_actif].nom_calendrier.Count);
                                projet.reseaux[projet.reseau_actif].nom_calendrier.Add(ch[8].ToString().Trim());

                            }

                            num_service.regime = projet.reseaux[projet.reseau_actif].num_calendrier[ch[8].ToString().Trim()];

                            int nb = projet.reseaux[projet.reseau_actif].links.Count;




                            /*dictionnaire lien*/
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
                                if (ch.Length > 9)
                                {
                                    if (ch[9].Length > 0)
                                    {
                                        lien.texte = ch[9];
                                    }
                                    else
                                    {
                                        lien.texte = " ";
                                    }
                                }
                                if (ch.Length > 10)
                                {

                                    lien.type = ch[10].Trim().ToString();
                                    if (types.Contains(lien.type) == false)
                                    {
                                        types.Add(lien.type);
                                    }

                                }
                                else
                                {

                                    lien.type = "0";
                                }

                                if (ch.Length > 11)
                                {
                                    if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                    {
                                        lien.toll = float.Parse(ch[11].Replace(',', '.'));
                                    }
                                    else
                                    {
                                        lien.toll = float.Parse(ch[11].Replace('.', ','));
                                    }
                                }



                                projet.reseaux[projet.reseau_actif].links.Add(lien);
                                link_id[num_link] = projet.reseaux[projet.reseau_actif].links.Count - 1;

                            }

                            /*                        if (nb > 0 )                        
                                                    {
                                                        if (projet.reseaux[projet.reseau_actif].links[nb - 1].no == projet.reseaux[projet.reseau_actif].numnoeud[ni] && projet.reseaux[projet.reseau_actif].links[nb - 1].nd == projet.reseaux[projet.reseau_actif].numnoeud[nj] && projet.reseaux[projet.reseau_actif].links[nb - 1].ligne == line && num_service.numero > 0)
                                                        {

                                                            projet.reseaux[projet.reseau_actif].links[nb - 1].services.Add(num_service);
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

                                                            if (num_service.numero >0)
                                                            {
                                                                lien.services.Add(num_service);
                                                                projet.reseaux[projet.reseau_actif].nbservices += 1;
                                                            }
                                                            if (ch.Length > 9)
                                                            {
                                                                lien.texte = ch[9];
                                                            }
                                                            if (ch.Length > 10)
                                                            {
                                                                lien.type = int.Parse(ch[10]);
                                                                if (lien.type > projet.reseaux[projet.reseau_actif].max_type)
                                                                {
                                                                    projet.reseaux[projet.reseau_actif].max_type = lien.type;

                                                                }
                                                            }

                                                            if (ch.Length > 11)
                                                            {
                                                                if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                                                {
                                                                    lien.toll = float.Parse(ch[11].Replace(',', '.'));
                                                                }
                                                                else
                                                                {
                                                                    lien.toll = float.Parse(ch[11].Replace('.', ','));
                                                                }
                                                            }



                                                            projet.reseaux[projet.reseau_actif].links.Add(lien);
                                                        }

                                                    }
                                                        else
                                                    {
                                                            lien.ligne = line;
                                                            if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                                            {
                                                                lien.temps = float.Parse(ch[2].Replace(',','.'));
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
                                                            if (ch.Length > 9)
                                                            {
                                                                lien.texte = ch[9];
                                                            }

                                                            if (ch.Length > 10)
                                                            {
                                                                lien.type = int.Parse(ch[10]);
                                                                if (lien.type > projet.reseaux[projet.reseau_actif].max_type)
                                                                {
                                                                    projet.reseaux[projet.reseau_actif].max_type = lien.type;

                                                                }
                                                            }
                                                            if (ch.Length > 11)
                                                            {
                                                                if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                                                {
                                                                    lien.toll = float.Parse(ch[11].Replace(',', '.'));
                                                                }
                                                                else
                                                                {
                                                                    lien.toll = float.Parse(ch[11].Replace('.', ','));
                                                                }
                                                            }



                                                            projet.reseaux[projet.reseau_actif].links.Add(lien);
                                                       }


                              */

                        }
                    }
                    fichier_reseau.Close();
                    flux_reseau.Close();

                    /*    for (int k = 0; k <= projet.reseaux[projet.reseau_actif].max_type; k++)
                        {
                            projet.param_affectation_horaire.cveh.Add(1f);
                            projet.param_affectation_horaire.coef_tmap.Add(1f);
                            projet.param_affectation_horaire.cmap.Add(1f);
                            projet.param_affectation_horaire.cboa.Add(1f);
                            projet.param_affectation_horaire.tboa.Add(1f);
                            projet.param_affectation_horaire.cwait.Add(1f);
                            projet.param_affectation_horaire.tboa_max.Add(1f);
                            projet.param_affectation_horaire.ctoll.Add(1f);

                        }*/

                    fich_log.WriteLine("Network:" + nom_reseau);
                    fich_log.WriteLine("Nodes:" + projet.reseaux[projet.reseau_actif].nodes.Count);
                    fich_log.WriteLine("Links:" + projet.reseaux[projet.reseau_actif].links.Count);

                    //construction du graphe
                    // table des prédécesseurs et successeurs de noeuds
                    avancement = 0;
                    Console.WriteLine();
                    ctop = Console.CursorTop;
                    cleft = Console.CursorLeft;

                    for (i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
                    {
                        //virage.distance = 0;
                        //virage.cout = 0;

                        projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].nd].pred.Add(i);
                        projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].no].succ.Add(i);
                        //                    Console.SetCursorPosition(1, Console.CursorTop-1);

                        if (avancement < (int)((100 * (i + 1)) / projet.reseaux[projet.reseau_actif].links.Count) - 4)
                        {
                            avancement = (int)((100 * (i + 1)) / projet.reseaux[projet.reseau_actif].links.Count);
                            Console.SetCursorPosition(cleft, ctop);
                            Console.Write("Network topology generation:" + ((100 * (i + 1)) / projet.reseaux[projet.reseau_actif].links.Count).ToString() + "%");

                        }

                    }

                    avancement = 0;

                    // table des prédécesseurs et successeurs de tronçons
                    //Console.WriteLine("création de la topologie des noeuds terminée");

                    Console.WriteLine();
                    ctop = Console.CursorTop;
                    cleft = Console.CursorLeft;

                    /*for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].no].pred.Count; j++)
                    {
                        turn virage = new turn();
                        int predecesseur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].no].pred[j];

                        {
                            virage.numero = predecesseur;
                            virage.temps = 0;
                            projet.reseaux[projet.reseau_actif].links[i].arci.Add(virage);
                            if (projet.reseaux[projet.reseau_actif].links[i].nd == projet.reseaux[projet.reseau_actif].links[predecesseur].no && projet.param_affectation_horaire.demitours == true)
                            {
                                projet.reseaux[projet.reseau_actif].links[i].arci[j].temps = -1;
                                projet.reseaux[projet.reseau_actif].links[i].arci[j].is_valid = true;
                            }
                        }

                    
                    }*/
                    /* for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].nd].succ.Count; j++)
                     {
                         turn virage = new turn();
                         int successeur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].nd].succ[j];
                         {
                             virage.numero = successeur;
                             virage.temps = 0;
                             projet.reseaux[projet.reseau_actif].links[i].arcj.Add(virage);
                             projet.reseaux[projet.reseau_actif].nbturns += 1;
                             if (projet.reseaux[projet.reseau_actif].links[i].no == projet.reseaux[projet.reseau_actif].links[successeur].nd && projet.param_affectation_horaire.demitours == true)
                             {
                                 projet.reseaux[projet.reseau_actif].links[i].arcj[j].temps = -1;
                                 projet.reseaux[projet.reseau_actif].links[i].arcj[j].is_valid = true;

                             }
                         }

                     }*/



                    //fich_log.WriteLine("Virages et correspondances:" + projet.reseaux[projet.reseau_actif].nbturns);
                    fich_log.WriteLine("Time based services:" + projet.reseaux[projet.reseau_actif].nbservices);

                    /*************************Import des pénalités et temps de correspondances************************/
                    /*************************Import des pénalités et temps de correspondances************************/
                    /*************************Import des pénalités et temps de correspondances************************/
                    /*************************Import des pénalités et temps de correspondances************************/
                    /*************************Import des pénalités et temps de correspondances************************/
                    /*************************Import des pénalités et temps de correspondances************************/
                    /*************************Import des pénalités et temps de correspondances************************/

                    if (System.IO.File.Exists(nom_penalites) == true && System.IO.File.Exists(nom_reseau) == true && System.IO.File.Exists(nom_matrice) == true && nom_reseau != null && nom_matrice != null)
                    {
                        fich_log.WriteLine("Penalties and transfers:" + nom_penalites);
                        string[] penal;
                        int ni, nj, nk;
                        int linei, linej, ntri, ntrj;
                        float tps_mvt;
                        System.IO.FileStream flux_penalites;
                        flux_penalites = new System.IO.FileStream(nom_penalites, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read);
                        System.IO.StreamReader fichier_penalites = new System.IO.StreamReader(flux_penalites, System.Text.Encoding.UTF8);
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
                            {
                                tps_mvt = float.Parse(penal[5].Replace(',', '.'));
                            }
                            else
                            {
                                tps_mvt = float.Parse(penal[5].Replace('.', ','));
                            }
                            for (i = 0; i < projet.reseaux[projet.reseau_actif].nodes[nj].pred.Count; i++)
                            {
                                if (projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[nj].pred[i]].no == ni && projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[nj].pred[i]].ligne == linei)
                                {
                                    ntri = projet.reseaux[projet.reseau_actif].nodes[nj].pred[i];
                                }
                            }
                            for (i = 0; i < projet.reseaux[projet.reseau_actif].nodes[nj].succ.Count; i++)
                            {
                                if (projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[nj].succ[i]].nd == nk && projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[nj].succ[i]].ligne == linej)
                                {
                                    ntrj = projet.reseaux[projet.reseau_actif].nodes[nj].succ[i];
                                }
                            }
                            if (ntrj >= 0 && ntri >= 0)
                            {
                                Turn virage = new Turn();
                                virage.arci = ntri;
                                virage.arcj = ntrj;
                                float value;
                                if (turns.TryGetValue(virage, out value) == false)
                                {
                                    turns.Add(virage, tps_mvt);
                                }
                                projet.reseaux[projet.reseau_actif].nodes[nj].is_intersection = true;
                                /*  for (i = 0; i < projet.reseaux[projet.reseau_actif].links[ntrj].arci.Count; i++)
                                  {
                                      if (projet.reseaux[projet.reseau_actif].links[ntrj].arci[i].numero == ntri)
                                      {
                                          projet.reseaux[projet.reseau_actif].links[ntrj].arci[i].temps = tps_mvt;
                                          projet.reseaux[projet.reseau_actif].links[ntrj].arci[i].is_valid = true;

                                      }
                                  }
                                  for (i = 0; i < projet.reseaux[projet.reseau_actif].links[ntri].arcj.Count; i++)
                                  {
                                      if (projet.reseaux[projet.reseau_actif].links[ntri].arcj[i].numero == ntrj)
                                      {
                                          projet.reseaux[projet.reseau_actif].links[ntri].arcj[i].temps = tps_mvt;
                                          projet.reseaux[projet.reseau_actif].links[ntri].arcj[i].is_valid = true;

                                      }
                                  }*/
                            }

                        }
                        fichier_penalites.Close();
                        flux_penalites.Close();

                    }
                    Console.SetCursorPosition(cleft, ctop);
                    Console.Write("Penalties and transfers import:" + (100).ToString() + "%");


                    ////écrire réseau en XML////

                    /*network Export = projet.reseaux[projet.reseau_actif];
                    System.Xml.Serialization.XmlSerializer writer =
                    new System.Xml.Serialization.XmlSerializer(Export.GetType());
                    System.IO.StreamWriter file = new System.IO.StreamWriter( projet.param_affectation_horaire.nom_sortie+ ".xml");

                    writer.Serialize(file, Export);
                    file.Close();*/



                    //affectation tc à horaires algorithme
                    // graph growth aglorithm with buckets
                    // graph growth aglorithm with buckets
                    // graph growth aglorithm with buckets
                    // graph growth aglorithm with buckets
                    // graph growth aglorithm with buckets
                    // graph growth aglorithm with buckets
                    // graph growth aglorithm with buckets
                    // graph growth aglorithm with buckets
                    if (projet.param_affectation_horaire.algorithme <= 1)
                    {


                        System.IO.StreamWriter fich_sortie = new System.IO.StreamWriter(projet.param_affectation_horaire.nom_sortie + "_temps.txt", false, Encoding.UTF8);
                        System.IO.StreamWriter fich_sortie2 = new System.IO.StreamWriter(projet.param_affectation_horaire.nom_sortie + "_chemins.txt", false, Encoding.UTF8);
                        System.IO.StreamWriter fich_result = new System.IO.StreamWriter(projet.param_affectation_horaire.nom_sortie + "_aff.txt", false, Encoding.UTF8);
                        System.IO.StreamWriter fich_od = new System.IO.StreamWriter(projet.param_affectation_horaire.nom_sortie + "_od.txt", false, Encoding.UTF8);
                        System.IO.StreamWriter fich_noeuds = new System.IO.StreamWriter(projet.param_affectation_horaire.nom_sortie + "_noeuds.txt", false, Encoding.UTF8);
                        System.IO.StreamWriter fich_detour = new System.IO.StreamWriter(projet.param_affectation_horaire.nom_sortie + "_detour.txt", false, Encoding.UTF8);
                        System.IO.StreamWriter fich_isoles = new System.IO.StreamWriter(projet.param_affectation_horaire.nom_sortie + "_isoles.txt", false, Encoding.UTF8);
                        Ecrit_parametres(projet.param_affectation_horaire, projet.param_affectation_horaire.nom_sortie + "_param.txt");
                        fich_sortie2.WriteLine("id;o;d;jour;heure;i;j;ij;ligne;service;temps;heureo;tveh;tmap;tatt;tcorr;ncorr;tatt1;cout;longueur;pole;volau;boai;alij;texte;type;toll");
                        fich_result.WriteLine("i;j;ligne;volau;boai;alij;texte;type;toll");
                        fich_od.WriteLine("id;o;d;jour;heureo;heured;temps;tveh;tmap;tatt;tcorr;ncorr;tatt1;cout;longueur;pole;volau;texte;nbpop;toll");
                        if (projet.param_affectation_horaire.sortie_temps == 3)
                        {
                            fich_sortie.WriteLine("o;ij;ligne;temps;tatt1;volau");
                            fich_noeuds.WriteLine("o;numero;temps;tatt1;volau");

                        }
                        else
                        {
                            fich_sortie.WriteLine("id;o;ij;ligne;numtrc;jour;heureo;heured;temps;tveh;tmap;tatt;tcorr;ncorr;tatt1;cout;longueur;pole;volau;precedent;type;toll;ti");
                            fich_noeuds.WriteLine("id;o;d;jour;numero;heureo;heured;temps;tveh;tmap;tatt;tcorr;ncorr;tatt1;cout;longueur;pole;toll;volau;stops");
                        }



                        // Console.WriteLine("création de la topologie des tronçons terminée");
                        //plus courts chemins
                        Queue<int> touches = new Queue<int>();
                        Queue<int> calcules = new Queue<int>();
                        List<List<int>> gga_nq = new List<List<int>>();

                        avancement = 0;
                        Console.WriteLine();
                        ctop = Console.CursorTop;
                        cleft = Console.CursorLeft;


                        //initilisation
                        for (i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
                        {
                            projet.reseaux[projet.reseau_actif].links[i].l = 0;
                            projet.reseaux[projet.reseau_actif].links[i].volau = 0;
                            projet.reseaux[projet.reseau_actif].links[i].touche = 0;
                            projet.reseaux[projet.reseau_actif].links[i].cout = 0;
                            projet.reseaux[projet.reseau_actif].links[i].pivot = -1;
                            projet.reseaux[projet.reseau_actif].links[i].is_queue = false;
                            //                projet.reseaux[projet.reseau_actif].links[i].temps = projet.reseaux[projet.reseau_actif].links[i].fd(projet.reseaux[projet.reseau_actif].links[i].volau, projet.reseaux[projet.reseau_actif].links[i].longueur, 0f, projet.reseaux[projet.reseau_actif].links[i].lanes * 1000, projet.reseaux[projet.reseau_actif].links[i].v0, projet.reseaux[projet.reseau_actif].links[i].a, projet.reseaux[projet.reseau_actif].links[i].b, projet.reseaux[projet.reseau_actif].links[i].n);

                        }



                        // ─────────────────────────────────────────────────────────────────
                        // PARALLÉLISATION : lecture matrice + Parallel.ForEach par groupe
                        // ─────────────────────────────────────────────────────────────────

                        // 1. Lire toute la matrice en mémoire
                        var toutes_paires = new List<string>();
                        {
                            using var fmx = new System.IO.FileStream(nom_matrice, System.IO.FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                            using var fmr = new System.IO.StreamReader(fmx, System.Text.Encoding.UTF8);
                            string ln2;
                            while ((ln2 = fmr.ReadLine()) != null)
                                if (ln2.Trim().Length > 0) toutes_paires.Add(ln2);
                        }
                        fich_log.WriteLine("Matrix:" + nom_matrice);
                        DateTime t1 = DateTime.Now;
                        fich_log.WriteLine("Computation start time: " + t1.ToString("dddd dd MMMM yyyy HH:mm:ss.fff"));
                        fich_log.Flush();

                        // 2. Grouper par (p, jour, horaire, sens)
                        int numod_counter = 0;
                        var groupes_od = new List<List<(string chaine, int numod)>>();
                        var key_to_idx = new Dictionary<(string, int, float, int), int>();

                        foreach (var chaine_od in toutes_paires)
                        {
                            numod_counter++;
                            var ch0 = chaine_od.Split(param, StringSplitOptions.RemoveEmptyEntries);
                            if (ch0.Length < 5) continue;
                            string p0 = ch0[0].Trim();
                            var ci2 = System.Globalization.CultureInfo.InvariantCulture;
                            int jour0 = (int)float.Parse(ch0[3].Replace(',', '.'), ci2);
                            float hor0 = float.Parse(ch0[4].Replace(',', '.'), ci2);
                            int sens0 = (ch0.Length > 5 && ch0[5].ToLower() == "a") ? 2 : 1;
                            // Paires avec params spécifiques (ch0.Length>17) -> groupe unique
                            bool specific = ch0.Length > 17;
                            var key2 = specific
                                ? ("__" + numod_counter, jour0, hor0, sens0)
                                : (p0, jour0, hor0, sens0);
                            if (!key_to_idx.TryGetValue(key2, out int gidx))
                            {
                                gidx = groupes_od.Count;
                                groupes_od.Add(new List<(string, int)>());
                                key_to_idx[key2] = gidx;
                            }
                            groupes_od[gidx].Add((chaine_od, numod_counter));
                        }

                        int total_gr = Math.Max(1, groupes_od.Count);
                        int gr_done = 0;
                        int ctop2 = Console.CursorTop; int cleft2 = Console.CursorLeft;
                        Console.Write("Shortest paths computing...:0%  ");

                        using var timer_av = new System.Threading.Timer(_ => {
                            int dd = Volatile.Read(ref gr_done);
                            Console.SetCursorPosition(cleft2, ctop2);
                            Console.Write("Shortest paths computing...:" + (100 * dd / total_gr) + "%  ");
                        }, null, 200, 200);

                        // 3. Tableau de résultats indexé par groupe
                        var resultats = new LignesOD[groupes_od.Count];

                        // 4. Parallel.ForEach
                        int nb_links_tot = projet.reseaux[projet.reseau_actif].links.Count;
                        var thread_st = new ThreadLocal<LinkState[]>(() => {
                            var arr = new LinkState[nb_links_tot];
                            for (int ii = 0; ii < nb_links_tot; ii++) arr[ii] = new LinkState();
                            return arr;
                        });
                        var thread_delta = new ThreadLocal<float[][]>(() => {
                            var arr2 = new float[nb_links_tot][];
                            for (int ii = 0; ii < nb_links_tot; ii++)
                            {
                                int nsvc = projet.reseaux[projet.reseau_actif].links[ii].services.Count;
                                arr2[ii] = nsvc > 0 ? new float[nsvc] : Array.Empty<float>();
                            }
                            return arr2;
                        });

                        Parallel.ForEach(
                            Enumerable.Range(0, groupes_od.Count),
                            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) },
                            g_idx =>
                            {
                                var st = thread_st.Value;
                                var st_delta = thread_delta.Value;

                                // Réinitialiser l'état
                                for (int ii = 0; ii < nb_links_tot; ii++)
                                {
                                    var s = st[ii];
                                    s.cout = 0; s.h = 0; s.tatt = 0; s.tatt1 = 0; s.tcor = 0;
                                    s.tmap = 0; s.tveh = 0; s.ttoll = 0; s.l = 0; s.ncorr = 0;
                                    s.boai = 0; s.alij = 0; s.volau = 0;
                                    s.touche = 0; s.pivot = -1; s.turn_pivot = -1; s.service = -1;
                                    s.is_queue = false; s.pole = "-1"; s.poleV2 = "";
                                    int nsvc2 = projet.reseaux[projet.reseau_actif].links[ii].services.Count;
                                    if (nsvc2 > 0) Array.Clear(st_delta[ii], 0, nsvc2);
                                }

                                var lignes_gr = new LignesOD();
                                resultats[g_idx] = lignes_gr;
                                int nb_pop_local = 0;
                                string p1_loc = "", q1_loc = "";
                                int sens1_loc = 0, jour1_loc = 0;
                                float horaire1_loc = 0;
                                var gga_nq = new List<List<int>>();
                                int id_bucket = 0;

                                // svc_tmp[linkIdx][svcIdx] : accumulations temporaires par service
                                var svc_tmp = new SvcAccum[nb_links_tot][];
                                for (int ii2 = 0; ii2 < nb_links_tot; ii2++)
                                {
                                    int nsvc3 = projet.reseaux[projet.reseau_actif].links[ii2].services.Count;
                                    svc_tmp[ii2] = new SvcAccum[nsvc3];
                                    for (int jj2 = 0; jj2 < nsvc3; jj2++)
                                        svc_tmp[ii2][jj2] = new SvcAccum();
                                }


                                foreach (var (chaine_od, numod_od) in groupes_od[g_idx])
                                {
                                    string chaine = chaine_od;
                                    int numod = numod_od;
                                    string _chaine_od = chaine_od;
                                    int _numod = numod_od;
                                    string[] ch;
                                    string p = "", q = "", libod = "";
                                    float od = 0, horaire = 0;
                                    int jour = 0, sens = 1;
                                    //string texte = "";

                                    // Reset svc_tmp pour cette paire
                                    for (int ii3 = 0; ii3 < nb_links_tot; ii3++)
                                        for (int jj3 = 0; jj3 < svc_tmp[ii3].Length; jj3++)
                                        {
                                            svc_tmp[ii3][jj3].volau = 0;
                                            svc_tmp[ii3][jj3].boai = 0;
                                            svc_tmp[ii3][jj3].boat = 0;
                                            svc_tmp[ii3][jj3].alij = 0;
                                            svc_tmp[ii3][jj3].alit = 0;
                                        }


                                    nb_pop_local = 0;
                                    chaine = _chaine_od;
                                    numod = _numod;
                                    ch = chaine.Split(param, StringSplitOptions.RemoveEmptyEntries);
                                    p = ch[0].Trim();
                                    q = ch[1].Trim();
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
                                        {
                                            sens = 1;

                                        }
                                        else if (ch[5].ToLower() == "a")
                                        {
                                            sens = 2;
                                        }
                                    }
                                    else
                                    {
                                        sens = 1;
                                    }
                                    if (ch.Length > 6)
                                    {
                                        if (ch[6].Length == 0)
                                        {
                                            libod = numod.ToString();

                                        }
                                        else
                                        {
                                            libod = ch[6].Trim();
                                        }
                                        libod = ch[6];
                                    }
                                    else
                                    {
                                        libod = numod.ToString();
                                    }
                                    if (ch.Length > 17)
                                    {
                                        string[] type_delim = { "|" };
                                        int k;
                                        string[] scveh, scwait, scmap, scboa, scoef_tmap, stboa, stboa_max, stoll;
                                        if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                        {
                                            scveh = ch[7].Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            scwait = ch[8].Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            scmap = ch[9].Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            scboa = ch[10].Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            scoef_tmap = ch[11].Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            stboa = ch[12].Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            stboa_max = ch[13].Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            //    stmap_max = ch[13].Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            stoll = ch[16].Replace(",", ".").Split(type_delim, StringSplitOptions.None);

                                        }
                                        else
                                        {
                                            scveh = ch[7].Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            scwait = ch[8].Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            scmap = ch[9].Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            scboa = ch[10].Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            scoef_tmap = ch[11].Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            stboa = ch[12].Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            stboa_max = ch[13].Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            //  stmap_max = ch[13].Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            stoll = ch[16].Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                        }
                                        projet.param_affectation_horaire.texte_cveh = ch[7];
                                        projet.param_affectation_horaire.texte_cwait = ch[8];
                                        projet.param_affectation_horaire.texte_cmap = ch[9];
                                        projet.param_affectation_horaire.texte_cboa = ch[10];
                                        projet.param_affectation_horaire.texte_coef_tmap = ch[11];
                                        projet.param_affectation_horaire.texte_tboa = ch[12];
                                        projet.param_affectation_horaire.texte_tboa_max = ch[13];
                                        //                        projet.param_affectation_horaire.texte_tboa_max = ch[12];
                                        projet.param_affectation_horaire.texte_toll = ch[16];


                                        //pondérations temps TC par type
                                        for (k = 0; k < scveh.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scveh[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.cveh["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.cveh[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondérations temps attente par type
                                        for (k = 0; k < scwait.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scwait[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.cwait["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.cwait[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondérations temps marche par type
                                        for (k = 0; k < scmap.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scmap[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.cmap["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.cmap[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondérations correspondance par type
                                        for (k = 0; k < scboa.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scboa[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.cboa["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.cboa[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondérations coef vitesse marche par type
                                        for (k = 0; k < scoef_tmap.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scoef_tmap[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.coef_tmap["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.coef_tmap[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //temps correspondance par type
                                        for (k = 0; k < stboa.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = stboa[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.tboa["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.tboa[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //temps correspondance maximum par type
                                        for (k = 0; k < stboa_max.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = stboa_max[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.tboa_max["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.tboa_max[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondération péage par type
                                        for (k = 0; k < stoll.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = stoll[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.ctoll["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.ctoll[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }

                                        foreach (String cle in types)
                                        {
                                            if (projet.param_affectation_horaire.cveh.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.cveh[cle] = projet.param_affectation_horaire.cveh["0"];
                                            }
                                            if (projet.param_affectation_horaire.cmap.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.cmap[cle] = projet.param_affectation_horaire.cmap["0"];
                                            }
                                            if (projet.param_affectation_horaire.cwait.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.cwait[cle] = projet.param_affectation_horaire.cwait["0"];
                                            }
                                            if (projet.param_affectation_horaire.cboa.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.cboa[cle] = projet.param_affectation_horaire.cboa["0"];
                                            }
                                            if (projet.param_affectation_horaire.tboa.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.tboa[cle] = projet.param_affectation_horaire.tboa["0"];
                                            }
                                            if (projet.param_affectation_horaire.coef_tmap.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.coef_tmap[cle] = projet.param_affectation_horaire.coef_tmap["0"];
                                            }
                                            if (projet.param_affectation_horaire.tboa_max.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.tboa_max[cle] = projet.param_affectation_horaire.tboa_max["0"];
                                            }
                                            if (projet.param_affectation_horaire.ctoll.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.ctoll[cle] = projet.param_affectation_horaire.ctoll["0"];
                                            }
                                        }
                                        if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                        {
                                            projet.param_affectation_horaire.nb_jours = int.Parse(ch[14].Split(type_delim, StringSplitOptions.None)[0]);
                                            projet.param_affectation_horaire.tmapmax = float.Parse(ch[15].Replace(',', '.').Split(type_delim, StringSplitOptions.None)[0]);
                                            projet.param_affectation_horaire.temps_max = float.Parse(ch[17].Replace(',', '.').Split(type_delim, StringSplitOptions.None)[0]);

                                        }
                                        else
                                        {
                                            projet.param_affectation_horaire.nb_jours = int.Parse(ch[14].Split(type_delim, StringSplitOptions.None)[0]);
                                            projet.param_affectation_horaire.tmapmax = float.Parse(ch[15].Replace('.', ',').Split(type_delim, StringSplitOptions.None)[0]);
                                            projet.param_affectation_horaire.temps_max = float.Parse(ch[17].Replace('.', ',').Split(type_delim, StringSplitOptions.None)[0]);

                                        }

                                        /*                        for (k = 0; k <= projet.reseaux[projet.reseau_actif].max_type; k++)
                                                                {



                                                                    if (k < scveh.Length)
                                                                    {
                                                                        projet.param_affectation_horaire.cveh[k]= float.Parse(scveh[k]);
                                                                    }
                                                                    else
                                                                    {
                                                                        projet.param_affectation_horaire.cveh[k] = float.Parse(scveh[0]);
                                                                    }
                                                                    if (k < scwait.Length)
                                                                    {
                                                                        projet.param_affectation_horaire.cwait[k]= float.Parse(scwait[k]);
                                                                    }
                                                                    else
                                                                    {
                                                                        projet.param_affectation_horaire.cwait[k] = float.Parse(scwait[0]);
                                                                    }
                                                                    if (k < scmap.Length)
                                                                    {
                                                                        projet.param_affectation_horaire.cmap[k]= float.Parse(scmap[k]);
                                                                    }
                                                                    else
                                                                    {
                                                                        projet.param_affectation_horaire.cmap[k] = float.Parse(scmap[0]);
                                                                    }
                                                                    if (k < scboa.Length)
                                                                    {
                                                                        projet.param_affectation_horaire.cboa[k]= float.Parse(scboa[k]);
                                                                    }
                                                                    else
                                                                    {
                                                                        projet.param_affectation_horaire.cboa[k] = float.Parse(scboa[0]);
                                                                    }
                                                                    if (k < scoef_tmap.Length)
                                                                    {
                                                                        projet.param_affectation_horaire.coef_tmap[k]= float.Parse(scoef_tmap[k]);
                                                                    }
                                                                    else
                                                                    {
                                                                        projet.param_affectation_horaire.coef_tmap[k] = float.Parse(scoef_tmap[0]);
                                                                    }
                                                                    if (k < stboa.Length)
                                                                    {
                                                                        projet.param_affectation_horaire.tboa[k]= float.Parse(stboa[k]);
                                                                    }
                                                                    else
                                                                    {
                                                                        projet.param_affectation_horaire.tboa[k] = float.Parse(stboa[0]);
                                                                    }
                                                                    if (k < stboa_max.Length)
                                                                    {
                                                                        projet.param_affectation_horaire.tboa_max[k] = float.Parse(stboa_max[k]);
                                                                    }
                                                                    else
                                                                    {
                                                                        projet.param_affectation_horaire.tboa_max[k] = float.Parse(stboa_max[0]);
                                                                    }
                                                                    if (k < stoll.Length)
                                                                    {
                                                                        projet.param_affectation_horaire.ctoll[k] = float.Parse(stoll[k]);
                                                                    }
                                                                    else
                                                                    {
                                                                        projet.param_affectation_horaire.ctoll[k] = float.Parse(stoll[0]);
                                                                    }
                                                                    projet.param_affectation_horaire.nb_jours = int.Parse(ch[13].Split(type_delim, StringSplitOptions.None)[0]);
                                                                    projet.param_affectation_horaire.tmapmax = int.Parse(ch[14].Split(type_delim, StringSplitOptions.None)[0]);


                                                                }*/

                                    }
                                    else
                                    {
                                        string[] type_delim = { "|" };
                                        int k;
                                        string[] scveh, scwait, scmap, scboa, scoef_tmap, stboa, stboa_max, stoll;
                                        if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                        {
                                            scveh = aff_hor.texte_cveh.Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            scwait = aff_hor.texte_cwait.Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            scmap = aff_hor.texte_cmap.Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            scboa = aff_hor.texte_cboa.Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            scoef_tmap = aff_hor.texte_coef_tmap.Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            stboa = aff_hor.texte_tboa.Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            stboa_max = aff_hor.texte_tboa_max.Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                            stoll = aff_hor.texte_toll.Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                        }
                                        else
                                        {
                                            scveh = aff_hor.texte_cveh.Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            scwait = aff_hor.texte_cwait.Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            scmap = aff_hor.texte_cmap.Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            scboa = aff_hor.texte_cboa.Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            scoef_tmap = aff_hor.texte_coef_tmap.Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            stboa = aff_hor.texte_tboa.Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            stboa_max = aff_hor.texte_tboa_max.Replace(".", ",").Split(type_delim, StringSplitOptions.None);
                                            stoll = aff_hor.texte_toll.Replace(",", ".").Split(type_delim, StringSplitOptions.None);
                                        }



                                        projet.param_affectation_horaire = aff_hor;
                                        //pondérations temps TC par type
                                        for (k = 0; k < scveh.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scveh[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.cveh["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.cveh[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondérations temps attente par type
                                        for (k = 0; k < scwait.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scwait[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.cwait["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.cwait[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondérations temps marche par type
                                        for (k = 0; k < scmap.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scmap[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.cmap["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.cmap[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondérations correspondance par type
                                        for (k = 0; k < scboa.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scboa[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.cboa["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.cboa[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondérations coef vitesse marche par type
                                        for (k = 0; k < scoef_tmap.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = scoef_tmap[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.coef_tmap["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.coef_tmap[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //temps correspondance par type
                                        for (k = 0; k < stboa.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = stboa[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.tboa["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.tboa[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //temps correspondance maximum par type
                                        for (k = 0; k < stboa_max.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = stboa_max[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.tboa_max["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.tboa_max[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }
                                        //pondération péage par type
                                        for (k = 0; k < stoll.Length; k++)
                                        {
                                            string[] keys;
                                            string[] sep = { ":" };
                                            keys = stoll[k].Split(sep, StringSplitOptions.None);
                                            if (keys.Length == 1)
                                            {
                                                projet.param_affectation_horaire.ctoll["0"] = float.Parse(keys[0]);
                                            }
                                            else
                                            {
                                                projet.param_affectation_horaire.ctoll[keys[0].Trim()] = float.Parse(keys[1]);
                                            }
                                        }

                                        foreach (String cle in types)
                                        {
                                            if (projet.param_affectation_horaire.cveh.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.cveh[cle] = projet.param_affectation_horaire.cveh["0"];
                                            }
                                            if (projet.param_affectation_horaire.cmap.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.cmap[cle] = projet.param_affectation_horaire.cmap["0"];
                                            }
                                            if (projet.param_affectation_horaire.cwait.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.cwait[cle] = projet.param_affectation_horaire.cwait["0"];
                                            }
                                            if (projet.param_affectation_horaire.cboa.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.cboa[cle] = projet.param_affectation_horaire.cboa["0"];
                                            }
                                            if (projet.param_affectation_horaire.tboa.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.tboa[cle] = projet.param_affectation_horaire.tboa["0"];
                                            }
                                            if (projet.param_affectation_horaire.coef_tmap.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.coef_tmap[cle] = projet.param_affectation_horaire.coef_tmap["0"];
                                            }
                                            if (projet.param_affectation_horaire.tboa_max.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.tboa_max[cle] = projet.param_affectation_horaire.tboa_max["0"];
                                            }
                                            if (projet.param_affectation_horaire.ctoll.ContainsKey(cle) == false)
                                            {
                                                projet.param_affectation_horaire.ctoll[cle] = projet.param_affectation_horaire.ctoll["0"];
                                            }
                                        }


                                        /*                            for (k = 0; k <= projet.reseaux[projet.reseau_actif].max_type; k++)
                                                                    {


                                                                        if (k < scveh.Length)
                                                                        {
                                                                            projet.param_affectation_horaire.cveh[k]= float.Parse(scveh[k]);
                                                                        }
                                                                        else
                                                                        {
                                                                            projet.param_affectation_horaire.cveh[k]=float.Parse(scveh[0]);
                                                                        }
                                                                        if (k <scwait.Length)
                                                                        {
                                                                            projet.param_affectation_horaire.cwait[k]= float.Parse(scwait[k]);
                                                                        }
                                                                        else
                                                                        {
                                                                            projet.param_affectation_horaire.cwait[k]= float.Parse(scwait[0]);
                                                                        }
                                                                        if (k < scmap.Length)
                                                                        {
                                                                            projet.param_affectation_horaire.cmap[k]= float.Parse(scmap[k]);
                                                                        }
                                                                        else
                                                                        {
                                                                            projet.param_affectation_horaire.cmap[k]= float.Parse(scmap[0]);
                                                                        }
                                                                        if (k < scboa.Length)
                                                                        {
                                                                            projet.param_affectation_horaire.cboa[k]= float.Parse(scboa[k]);
                                                                        }
                                                                        else
                                                                        {
                                                                            projet.param_affectation_horaire.cboa[k]= float.Parse(scboa[0]);
                                                                        }
                                                                        if (k < scoef_tmap.Length)
                                                                        {
                                                                            projet.param_affectation_horaire.coef_tmap[k]= float.Parse(scoef_tmap[k]);
                                                                        }
                                                                        else
                                                                        {
                                                                            projet.param_affectation_horaire.coef_tmap[k]= float.Parse(scoef_tmap[0]);
                                                                        }
                                                                        if (k < stboa.Length)
                                                                        {
                                                                            projet.param_affectation_horaire.tboa[k]= float.Parse(stboa[k]);
                                                                        }
                                                                        else
                                                                        {
                                                                            projet.param_affectation_horaire.tboa[k]=float.Parse(stboa[0]);                                
                                                                        }
                                                                        if (k < stboa_max.Length)
                                                                        {
                                                                            projet.param_affectation_horaire.tboa_max[k] = float.Parse(stboa_max[k]);
                                                                        }
                                                                        else
                                                                        {
                                                                            projet.param_affectation_horaire.tboa_max[k] = float.Parse(stboa_max[0]);
                                                                        }
                                                                        if (k < stoll.Length)
                                                                        {
                                                                            projet.param_affectation_horaire.ctoll[k] = float.Parse(stoll[k]);
                                                                        }
                                                                        else
                                                                        {
                                                                            projet.param_affectation_horaire.ctoll[k] = float.Parse(stoll[0]);
                                                                        }

                                                                    }

                                                                    */


                                    }
                                    if (ch.Length > 23)
                                    {
                                        if (System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator == ".")
                                        {
                                            projet.param_affectation_horaire.sortie_chemins = bool.Parse(ch[18].Replace(",", "."));
                                            projet.param_affectation_horaire.sortie_temps = int.Parse(ch[19].Replace(",", "."));
                                            projet.param_affectation_horaire.algorithme = int.Parse(ch[20].Replace(",", "."));
                                            projet.param_affectation_horaire.param_dijkstra = float.Parse(ch[21].Replace(",", "."));
                                            projet.param_affectation_horaire.max_nb_buckets = float.Parse(ch[22].Replace(",", "."));
                                            projet.param_affectation_horaire.pu = float.Parse(ch[23].Replace(",", "."));
                                        }
                                        else
                                        {
                                            projet.param_affectation_horaire.sortie_chemins = bool.Parse(ch[18].Replace(".", ","));
                                            projet.param_affectation_horaire.sortie_temps = int.Parse(ch[19].Replace(".", ","));
                                            projet.param_affectation_horaire.algorithme = int.Parse(ch[20].Replace(".", ","));
                                            projet.param_affectation_horaire.param_dijkstra = float.Parse(ch[21].Replace(".", ","));
                                            projet.param_affectation_horaire.max_nb_buckets = float.Parse(ch[22].Replace(".", ","));
                                            projet.param_affectation_horaire.pu = float.Parse(ch[23].Replace(".", ","));

                                        }

                                    }
                                    else
                                    {
                                        projet.param_affectation_horaire.sortie_chemins = aff_hor.sortie_chemins;
                                        projet.param_affectation_horaire.sortie_temps = aff_hor.sortie_temps;
                                        if (aff_hor.sortie_temps >= 10)
                                        {
                                            aff_hor.sortie_stops = true;
                                            aff_hor.sortie_temps += -10;
                                        }
                                        else
                                        {
                                            aff_hor.sortie_stops = false;
                                        }
                                        projet.param_affectation_horaire.algorithme = aff_hor.algorithme;
                                        projet.param_affectation_horaire.param_dijkstra = aff_hor.param_dijkstra;
                                        projet.param_affectation_horaire.max_nb_buckets = aff_hor.max_nb_buckets;
                                        projet.param_affectation_horaire.pu = aff_hor.pu;

                                    }


                                    if (ch.Length > 24)
                                    {
                                        projet.param_affectation_horaire.texte_filtre_sortie = ch[24];
                                    }
                                    //MessageBox.Show(p.ToString() + " " + q.ToString() + " " + horaire.ToString());
                                    //avancement.textBox1.Text = p.ToString() + " " + q.ToString() + " " + horaire.ToString();
                                    //                        avancement.textBox1.Text = flux.Position;
                                    //             fich_sortie.WriteLine(pivot.ToString() + projet.reseaux[projet.reseaux].links[pivot].cout.ToString());
                                    //                flux.Position += chaine.Length;


                                    HashSet<String> filtre = new HashSet<String>();

                                    if (projet.param_affectation_horaire.texte_filtre_sortie.Trim().Length > 0)
                                    {
                                        ch = projet.param_affectation_horaire.texte_filtre_sortie.Split('|');

                                        for (int f = 0; f < ch.Length; f++)
                                        {
                                            if (filtre.Contains(ch[f].Trim()) == false)

                                                filtre.Add(ch[f].Trim());
                                        }
                                    }


                                    //sens heure de départ//
                                    //sens heure de départ//
                                    //sens heure de départ//
                                    //sens heure de départ//
                                    //sens heure de départ//


                                    //if (projet.reseaux[projet.reseau_actif].matrices[0].o[p].d.Count > 0)
                                    if (sens == 1)
                                    {
                                        if (p1_loc == p && jour1_loc == jour && horaire1_loc == horaire && sens1_loc == sens && ch.Length < 13)
                                        {
                                            q1_loc = q;

                                            goto fin_gga;
                                        }
                                        p1_loc = p; q1_loc = q; jour1_loc = jour; horaire1_loc = horaire; sens1_loc = sens;
                                        for (i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
                                        {
                                            st[i].pole = "-1";
                                            st[i].poleV2 = "";
                                            st[i].touche = 0;
                                            st[i].cout = 0;
                                            st[i].tatt = 0;
                                            st[i].tatt1 = 0;
                                            st[i].tcor = 0;
                                            st[i].ncorr = 0;
                                            st[i].tmap = 0;
                                            st[i].tveh = 0;
                                            st[i].h = 0;
                                            st[i].ttoll = 0;
                                            st[i].l = 0;
                                            for (j = 0; j < projet.reseaux[projet.reseau_actif].links[i].services.Count; j++)
                                            {
                                                st_delta[i][j] = 0;
                                            }
                                            st[i].pivot = -1;
                                            st[i].turn_pivot = -1;
                                            st[i].service = -1;
                                            st[i].is_queue = false;





                                        }
                                        gga_nq.Clear();
                                        string depart = p;
                                        int pivot = -1, value;
                                        int successeur, bucket;
                                        id_bucket = 0;
                                        String succ_type;
                                        float penalite = 0, temps_correspondance, max_correspondance;

                                        if (projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(p, out value) == true)
                                        {
                                            for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[depart]].succ.Count; j++)
                                            {
                                                successeur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[depart]].succ[j];
                                                succ_type = projet.reseaux[projet.reseau_actif].links[successeur].type;
                                                max_correspondance = projet.param_affectation_horaire.tboa_max[succ_type];





                                                if (projet.reseaux[projet.reseau_actif].links[successeur].ligne < 0 && projet.param_affectation_horaire.cmap[succ_type] > 0 && projet.reseaux[projet.reseau_actif].links[successeur].temps < projet.param_affectation_horaire.tmapmax)
                                                {
                                                    bool test_periode = false;

                                                    if (projet.reseaux[projet.reseau_actif].links[successeur].services.Count > 0)
                                                    {
                                                        int decal_jour = (int)Math.Floor(horaire / 1440f);
                                                        int kk;
                                                        for (kk = 0; kk < projet.reseaux[projet.reseau_actif].links[successeur].services.Count; kk++)
                                                        {
                                                            if (decal_jour <= projet.param_affectation_horaire.nb_jours)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && projet.reseaux[projet.reseau_actif].links[successeur].services[kk].hd + 1440f * decal_jour <= horaire && projet.reseaux[projet.reseau_actif].links[successeur].services[kk].hf + 1440f * decal_jour > horaire)
                                                                {
                                                                    test_periode = true;
                                                                    st[successeur].service = kk;
                                                                }
                                                            }
                                                        }

                                                    }
                                                    else
                                                    {
                                                        test_periode = true;
                                                    }
                                                    //touches.Enqueue(successeur);

                                                    if (test_periode == true)
                                                    {
                                                        st[successeur].touche = 1;
                                                        st[successeur].cout = projet.reseaux[projet.reseau_actif].links[successeur].temps * projet.param_affectation_horaire.coef_tmap[succ_type] * projet.param_affectation_horaire.cmap[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                        st[successeur].l = projet.reseaux[projet.reseau_actif].links[successeur].longueur;
                                                        st[successeur].tmap = projet.reseaux[projet.reseau_actif].links[successeur].temps * projet.param_affectation_horaire.coef_tmap[succ_type];
                                                        st[successeur].ttoll = projet.reseaux[projet.reseau_actif].links[successeur].toll;
                                                        st[successeur].h = horaire + projet.reseaux[projet.reseau_actif].links[successeur].temps * projet.param_affectation_horaire.coef_tmap[succ_type];
                                                        st[successeur].pivot = -1;
                                                        st[successeur].turn_pivot = -1;
                                                        st[successeur].pole = depart;
                                                        st[successeur].poleV2 = "";
                                                        //bucket = Convert.ToInt32(Math.Min((Math.Pow(st[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra)), projet.param_affectation_horaire.max_nb_buckets);
                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));

                                                        while (bucket >= gga_nq.Count)
                                                        {
                                                            gga_nq.Add(new List<int>());
                                                        }
                                                        gga_nq[bucket].Add(successeur);
                                                        nb_pop_local++;

                                                    }
                                                }
                                                else if (projet.param_affectation_horaire.cveh[succ_type] > 0)
                                                {
                                                    int ii, jj, num_service = -1, h3 = 0, delta, duree_periode;
                                                    float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                                    for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[successeur].services.Count; ii++)
                                                    {
                                                        delta = 0;
                                                        duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[ii].regime].Length;
                                                        if ((projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f < horaire) || projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[ii].regime].Substring(jour, 1) == "N")
                                                        {

                                                            h1 = 1e38f;
                                                            h2 = 1e38f;
                                                            h3 = -1;
                                                            for (jj = jour + 1; jj <= Math.Min(jour + projet.param_affectation_horaire.nb_jours, duree_periode - 1); jj++)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[ii].regime].Substring(jj, 1) == "O" && (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + (-jour + jj) * 24f * 60f < h1))
                                                                {
                                                                    h1 = projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + (-jour + jj) * 24f * 60f;
                                                                    h2 = (-jour + jj);
                                                                    h3 = jj;
                                                                }

                                                            }
                                                            if (h3 != -1)
                                                            {
                                                                st_delta[successeur][ii] = h2;
                                                            }
                                                            else
                                                            {
                                                                delta = -1;
                                                            }


                                                        }


                                                        if (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f - horaire < max_correspondance)
                                                        {
                                                            if (((projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f - horaire) * projet.param_affectation_horaire.cwait[succ_type]) + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type] < cout2 && delta > -1)
                                                            {
                                                                cout2 = (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f - horaire) * projet.param_affectation_horaire.cwait[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                                num_service = ii;

                                                            }
                                                        }

                                                    }
                                                    if (num_service != -1)
                                                    {
                                                        st[successeur].service = num_service;
                                                        st[successeur].cout = (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd + st_delta[successeur][num_service] * 1440f - horaire) * projet.param_affectation_horaire.cwait[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];

                                                        st[successeur].touche = 1;

                                                        st[successeur].h = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f;

                                                        //                                    st[successeur].tatt = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta - st[pivot].h;
                                                        st[successeur].tatt = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f - horaire;
                                                        st[successeur].tatt1 = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f - horaire;

                                                        st[successeur].tveh = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd;
                                                        st[successeur].tcor = 0;
                                                        st[successeur].ncorr = 1;
                                                        st[successeur].tmap = 0;
                                                        st[successeur].ttoll = projet.reseaux[projet.reseau_actif].links[successeur].toll;
                                                        st[successeur].l = projet.reseaux[projet.reseau_actif].links[successeur].longueur;
                                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));

                                                        while (bucket >= gga_nq.Count)
                                                        {
                                                            gga_nq.Add(new List<int>());
                                                        }
                                                        gga_nq[bucket].Add(successeur);
                                                        nb_pop_local++;
                                                        //                                touches.Enqueue(successeur);
                                                        st[successeur].pivot = -1;
                                                        st[successeur].turn_pivot = -1;
                                                        st[successeur].pole = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;
                                                        st[successeur].poleV2 = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;
                                                    }
                                                }

                                            }
                                        }
                                        else
                                        {
                                            fich_log.WriteLine("OD error " + libod + ":" + chaine + ": non existing origin node!");
                                            id_bucket = 1;
                                        }
                                        int bucket_cout_max = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(projet.param_affectation_horaire.temps_max / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                        //         MessageBox.Show(projet.param_affectation_horaire.algorithme.ToString());

                                        while (gga_nq.Count >= id_bucket && bucket_cout_max >= id_bucket)
                                        {

                                            while (gga_nq[id_bucket].Count == 0)
                                            {
                                                id_bucket++;
                                                if (id_bucket >= gga_nq.Count || id_bucket >= bucket_cout_max + 1)
                                                {
                                                    goto fin_gga;
                                                }
                                            }

                                            if (projet.param_affectation_horaire.algorithme == 0)
                                            {
                                                pivot = gga_nq[id_bucket][0];
                                                gga_nq[id_bucket].RemoveAt(0);
                                            }
                                            else
                                            {
                                                int k, id_pivot = -1; double cout_max = 1e38;
                                                for (k = 0; k <= gga_nq[id_bucket].Count; k++)
                                                {
                                                    if (st[gga_nq[id_bucket][k]].cout < cout_max)
                                                    {
                                                        cout_max = st[gga_nq[id_bucket][k]].cout;
                                                        id_pivot = k;
                                                    }
                                                }
                                                pivot = gga_nq[id_bucket][id_pivot];
                                                gga_nq[id_bucket].RemoveAt(id_pivot);
                                                st[pivot].touche = 3;
                                            }



                                            //avancement.textBox1.Text = touches.Count.ToString() + " " + calcules.Count.ToString() + " " + st[pivot].cout;
                                            //avancement.textBox1.Refresh();
                                            for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].nd].succ.Count; j++)
                                            {
                                                link troncon_succ = projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].nd].succ[j]];
                                                link troncon_pivot = projet.reseaux[projet.reseau_actif].links[pivot];
                                                successeur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].nd].succ[j];
                                                succ_type = projet.reseaux[projet.reseau_actif].links[successeur].type;

                                                if (projet.param_affectation_horaire.demitours == true)
                                                {

                                                    if (troncon_pivot.no == troncon_succ.nd)
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
                                                virage.arci = pivot;
                                                virage.arcj = successeur;
                                                float value2;
                                                if (projet.reseaux[projet.reseau_actif].nodes[troncon_pivot.nd].is_intersection == true)
                                                {
                                                    if (turns.TryGetValue(virage, out value2) == true)
                                                    {
                                                        penalite = turns[virage];
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
                                                        temps_correspondance = penalite;
                                                        max_correspondance = projet.param_affectation_horaire.tboa_max[succ_type];

                                                    }
                                                    else
                                                    {
                                                        temps_correspondance = projet.param_affectation_horaire.tboa[succ_type];
                                                        max_correspondance = projet.param_affectation_horaire.tboa_max[succ_type];
                                                    }
                                                    //successeurs touches pour la première fois
                                                    if (st[successeur].touche == 0)
                                                    {
                                                        // successeur marche à pied
                                                        if (projet.reseaux[projet.reseau_actif].links[successeur].ligne < 0 && projet.param_affectation_horaire.cmap[succ_type] > 0 && st[pivot].tmap + projet.reseaux[projet.reseau_actif].links[successeur].temps < projet.param_affectation_horaire.tmapmax)
                                                        {
                                                            bool test_periode = false;
                                                            st[successeur].service = -1;
                                                            if (projet.reseaux[projet.reseau_actif].links[successeur].services.Count > 0)
                                                            {
                                                                int decal_jour = (int)(Math.Floor((st[pivot].h + penalite) / 1440f));
                                                                for (int kk = 0; kk < projet.reseaux[projet.reseau_actif].links[successeur].services.Count; kk++)
                                                                {
                                                                    if (decal_jour <= projet.param_affectation_horaire.nb_jours)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && projet.reseaux[projet.reseau_actif].links[successeur].services[kk].hd + 1440f * decal_jour <= st[pivot].h + penalite && projet.reseaux[projet.reseau_actif].links[successeur].services[kk].hf + 1440f * decal_jour > st[pivot].h + penalite)
                                                                        {
                                                                            test_periode = true;
                                                                            st[successeur].service = kk;
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
                                                                st[successeur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].temps + penalite) * projet.param_affectation_horaire.coef_tmap[succ_type] * projet.param_affectation_horaire.cmap[succ_type]/*+ projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type]*/;
                                                                st[successeur].h = st[pivot].h + (projet.reseaux[projet.reseau_actif].links[successeur].temps) * projet.param_affectation_horaire.coef_tmap[succ_type] + penalite;
                                                                st[successeur].tatt = st[pivot].tatt;
                                                                st[successeur].tatt1 = st[pivot].tatt1;
                                                                st[successeur].tveh = st[pivot].tveh;
                                                                st[successeur].tcor = st[pivot].tcor;

                                                                st[successeur].ncorr = st[pivot].ncorr;
                                                                st[successeur].tmap = st[pivot].tmap + (penalite + projet.reseaux[projet.reseau_actif].links[successeur].temps) * projet.param_affectation_horaire.coef_tmap[succ_type];
                                                                st[successeur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[successeur].toll;
                                                                st[successeur].touche = 1;

                                                                st[successeur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[successeur].longueur;


                                                                //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));

                                                                while (bucket >= gga_nq.Count)
                                                                {
                                                                    gga_nq.Add(new List<int>());
                                                                }
                                                                gga_nq[bucket].Add(successeur);
                                                                nb_pop_local++;
                                                                //                                        touches.Enqueue(successeur);
                                                                st[successeur].pivot = pivot;
                                                                st[successeur].turn_pivot = j;

                                                                st[successeur].pole = st[pivot].pole;
                                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                                {
                                                                    st[successeur].poleV2 = st[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;

                                                                }
                                                                else
                                                                {
                                                                    st[successeur].poleV2 = st[pivot].poleV2;
                                                                }
                                                            }
                                                        }
                                                        //successeur TC même ligne
                                                        else if (projet.reseaux[projet.reseau_actif].links[successeur].ligne == projet.reseaux[projet.reseau_actif].links[pivot].ligne && projet.param_affectation_horaire.cveh[succ_type] > 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                        {
                                                            int ii, num_service = -1;
                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[successeur].services.Count; ii++)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].numero == projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].numero)
                                                                {
                                                                    if (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd >= projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].hf)
                                                                    {
                                                                        num_service = ii;
                                                                    }
                                                                }
                                                            }
                                                            //                                                if (num_service != -1 && projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd + projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta * 1440f >= st[pivot].h)
                                                            if (num_service != -1 && projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd >= projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].hf)
                                                            {
                                                                st[successeur].service = num_service;
                                                                st_delta[successeur][num_service] = projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta;
                                                                st[successeur].touche = 1;
                                                                st[successeur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f - st[pivot].h) * projet.param_affectation_horaire.cveh[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                                st[successeur].h = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f;
                                                                st[successeur].tatt = st[pivot].tatt;
                                                                st[successeur].tatt1 = st[pivot].tatt1;
                                                                st[successeur].tveh = st[pivot].tveh + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f - st[pivot].h;
                                                                st[successeur].tcor = st[pivot].tcor;
                                                                st[successeur].ncorr = st[pivot].ncorr;
                                                                st[successeur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[successeur].longueur;
                                                                st[successeur].tmap = st[pivot].tmap;
                                                                st[successeur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[successeur].toll;

                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));

                                                                //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                while (bucket >= gga_nq.Count)
                                                                {
                                                                    gga_nq.Add(new List<int>());
                                                                }
                                                                gga_nq[bucket].Add(successeur);
                                                                nb_pop_local++;
                                                                //touches.Enqueue(successeur);
                                                                st[successeur].pivot = pivot;
                                                                st[successeur].turn_pivot = j;
                                                                st[successeur].pole = st[pivot].pole;
                                                                st[successeur].poleV2 = st[pivot].poleV2;
                                                            }
                                                        }

                                                        //successeur TC lignes différentes
                                                        else if (projet.reseaux[projet.reseau_actif].links[successeur].ligne != projet.reseaux[projet.reseau_actif].links[pivot].ligne && projet.param_affectation_horaire.cveh[succ_type] > 0 && projet.reseaux[projet.reseau_actif].links[successeur].ligne > 0 /*&& projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0*/)
                                                        {
                                                            int ii, jj, num_service = -1, h3 = 0, duree_periode, delta;
                                                            float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;

                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[successeur].services.Count; ii++)
                                                            {
                                                                delta = 0;

                                                                duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[ii].regime].Length;

                                                                if ((projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f < st[pivot].h + temps_correspondance) || projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[ii].regime].Substring(jour, 1) == "N")
                                                                {

                                                                    h1 = 1e38f;
                                                                    h2 = 1e38f;
                                                                    h3 = -1;
                                                                    for (jj = jour + 1; jj <= Math.Min(jour + projet.param_affectation_horaire.nb_jours, duree_periode - 1); jj++)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[ii].regime].Substring(jj, 1) == "O" && (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + (-jour + jj) * 24f * 60f < h1) && (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + (-jour + jj) * 24f * 60f) - temps_correspondance > st[pivot].h)
                                                                        {
                                                                            h1 = projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + (-jour + jj) * 24f * 60f;
                                                                            h2 = (-jour + jj);
                                                                            h3 = jj;
                                                                        }

                                                                    }
                                                                    if (h3 != -1)
                                                                    {
                                                                        if (st_delta[successeur][ii] > h2 || st[successeur].touche == 0)
                                                                        {
                                                                            st_delta[successeur][ii] = h2;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        delta = -1;
                                                                    }


                                                                }


                                                                if ((projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f < st[pivot].h + max_correspondance) && (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f >= st[pivot].h + temps_correspondance))

                                                                {
                                                                    if (st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f - st[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + temps_correspondance * projet.param_affectation_horaire.cboa[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type] < cout2 && delta > -1)
                                                                    {
                                                                        cout2 = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f - st[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + temps_correspondance * projet.param_affectation_horaire.cboa[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                                        num_service = ii;

                                                                    }
                                                                }

                                                            }
                                                            if (num_service != -1)
                                                            {
                                                                st[successeur].service = num_service;
                                                                st[successeur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd + st_delta[successeur][num_service] * 1440f - st[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[succ_type]) + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];

                                                                st[successeur].touche = 1;

                                                                st[successeur].h = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f;
                                                                if (st[pivot].ncorr == 0)
                                                                {
                                                                    st[successeur].tatt1 = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f - st[pivot].h;
                                                                }
                                                                else
                                                                {
                                                                    st[successeur].tatt1 = st[pivot].tatt1;
                                                                }


                                                                st[successeur].tatt = st[pivot].tatt + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f - st[pivot].h;
                                                                st[successeur].tveh = st[pivot].tveh + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd;
                                                                st[successeur].tcor = st[pivot].tcor + temps_correspondance;
                                                                st[successeur].ncorr = st[pivot].ncorr + 1;
                                                                st[successeur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[successeur].longueur;
                                                                st[successeur].tmap = st[pivot].tmap;
                                                                st[successeur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[successeur].toll;

                                                                //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                while (bucket >= gga_nq.Count)
                                                                {
                                                                    gga_nq.Add(new List<int>());
                                                                }
                                                                gga_nq[bucket].Add(successeur);
                                                                nb_pop_local++;
                                                                st[successeur].pivot = pivot;
                                                                st[successeur].turn_pivot = j;

                                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                                {
                                                                    st[successeur].poleV2 = st[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;
                                                                }
                                                                else
                                                                {
                                                                    st[successeur].poleV2 = st[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;
                                                                }

                                                                if (st[pivot].pole == depart)
                                                                {
                                                                    st[successeur].pole = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;
                                                                }
                                                                else
                                                                {
                                                                    st[successeur].pole = st[pivot].pole;
                                                                }
                                                                /* if (st[successeur].tveh < 0)
                                                                 {
                                                                     //fich_sortie.WriteLine("30 " + pivot.ToString() + " " + st[pivot].cout.ToString() + " " + st[successeur].cout.ToString() + " " + projet.reseaux[projet.reseau_actif].links[pivot].ligne.ToString() + " " + projet.reseaux[projet.reseau_actif].links[successeur].ligne.ToString() + " " + st[pivot].h.ToString() + " " + st[successeur].h.ToString());
                                                                 }*/
                                                            }
                                                        }
                                                    }


                                                    //eléments déjà touchés
                                                    else if (st[successeur].touche == 1 || st[successeur].touche == 2)
                                                    {
                                                        int id_service = -1;
                                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));

                                                        //successeurs marche à pied
                                                        if (projet.reseaux[projet.reseau_actif].links[successeur].ligne < 0 && projet.param_affectation_horaire.cmap[succ_type] > 0 && st[pivot].tmap + projet.reseaux[projet.reseau_actif].links[successeur].temps < projet.param_affectation_horaire.tmapmax)
                                                        {
                                                            bool test_periode = false;

                                                            if (projet.reseaux[projet.reseau_actif].links[successeur].services.Count > 0)
                                                            {
                                                                int decal_jour = (int)(Math.Floor((st[pivot].h + penalite) / 1440f));
                                                                for (int kk = 0; kk < projet.reseaux[projet.reseau_actif].links[successeur].services.Count; kk++)
                                                                {
                                                                    if (decal_jour <= projet.param_affectation_horaire.nb_jours)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && projet.reseaux[projet.reseau_actif].links[successeur].services[kk].hd + 1440f * decal_jour <= st[pivot].h + penalite && projet.reseaux[projet.reseau_actif].links[successeur].services[kk].hf + 1440f * decal_jour > st[pivot].h + penalite)
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

                                                                if (st[successeur].cout > st[pivot].cout + (penalite + projet.reseaux[projet.reseau_actif].links[successeur].temps) * projet.param_affectation_horaire.coef_tmap[succ_type] * projet.param_affectation_horaire.cmap[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type])
                                                                {
                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Remove(successeur);
                                                                    st[successeur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].temps + penalite) * projet.param_affectation_horaire.coef_tmap[succ_type] * projet.param_affectation_horaire.cmap[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                                    st[successeur].h = st[pivot].h + (projet.reseaux[projet.reseau_actif].links[successeur].temps) * projet.param_affectation_horaire.coef_tmap[succ_type] + penalite;
                                                                    st[successeur].tatt = st[pivot].tatt;
                                                                    st[successeur].tatt1 = st[pivot].tatt1;
                                                                    st[successeur].tveh = st[pivot].tveh;
                                                                    st[successeur].tcor = st[pivot].tcor;
                                                                    st[successeur].ncorr = st[pivot].ncorr;
                                                                    st[successeur].tmap = st[pivot].tmap + (penalite + projet.reseaux[projet.reseau_actif].links[successeur].temps) * projet.param_affectation_horaire.coef_tmap[succ_type];
                                                                    st[successeur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[successeur].toll;

                                                                    st[successeur].touche = 2;
                                                                    st[successeur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[successeur].longueur;
                                                                    st[successeur].pivot = pivot;
                                                                    st[successeur].turn_pivot = j;
                                                                    st[successeur].pole = st[pivot].pole;


                                                                    if (projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                                    {
                                                                        st[successeur].poleV2 = st[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;

                                                                    }
                                                                    else
                                                                    {
                                                                        st[successeur].poleV2 = st[pivot].poleV2;
                                                                    }


                                                                    st[successeur].service = id_service;                                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), ;
                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Add(successeur);
                                                                    nb_pop_local++;
                                                                }
                                                            }

                                                        }
                                                        //successeurs TC même ligne
                                                        else if ((projet.reseaux[projet.reseau_actif].links[successeur].ligne == projet.reseaux[projet.reseau_actif].links[pivot].ligne && projet.param_affectation_horaire.cveh[succ_type] > 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0 && st[successeur].cout > st[pivot].cout))
                                                        {
                                                            int ii, num_service = -1;
                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[successeur].services.Count; ii++)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].numero == projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].numero)
                                                                {
                                                                    if (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd >= projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].hf)
                                                                    {
                                                                        num_service = ii;
                                                                    }
                                                                }


                                                            }

                                                            if (num_service != -1)
                                                            {
                                                                //                                                    if (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd + projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta * 1440f >= st[pivot].h)
                                                                if (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd >= projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].hf)
                                                                {

                                                                    if (st[successeur].cout > st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hf + projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta * 1440f - st[pivot].h) * projet.param_affectation_horaire.cveh[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type] && projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd >= projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].hf)
                                                                    {
                                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                        gga_nq[bucket].Remove(successeur);
                                                                        st_delta[successeur][num_service] = projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta;
                                                                        st[successeur].service = num_service;
                                                                        st[successeur].touche = 2;
                                                                        st[successeur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f - st[pivot].h) * projet.param_affectation_horaire.cveh[succ_type] + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                                        st[successeur].h = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f;
                                                                        st[successeur].tatt = st[pivot].tatt;
                                                                        st[successeur].tatt1 = st[pivot].tatt1;
                                                                        st[successeur].tveh = st[pivot].tveh + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf /* + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f */- st[pivot].h;
                                                                        st[successeur].tcor = st[pivot].tcor;
                                                                        st[successeur].ncorr = st[pivot].ncorr;
                                                                        st[successeur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[successeur].longueur;
                                                                        st[successeur].tmap = st[pivot].tmap;
                                                                        st[successeur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[successeur].toll;

                                                                        st[successeur].pivot = pivot;

                                                                        st[successeur].turn_pivot = j;
                                                                        st[successeur].pole = st[pivot].pole;
                                                                        st[successeur].poleV2 = st[pivot].poleV2;
                                                                        //bucket = Convert.ToInt32(Math.Min((Math.Pow(st[successeur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                        gga_nq[bucket].Add(successeur);
                                                                        nb_pop_local++;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        //successeurs TC lignes différentes
                                                        else if ((projet.reseaux[projet.reseau_actif].links[successeur].ligne != projet.reseaux[projet.reseau_actif].links[pivot].ligne) && /*projet.reseaux[projet.reseau_actif].links[successeur].ligne>0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0 && */projet.param_affectation_horaire.cveh[succ_type] > 0 && st[successeur].cout > st[pivot].cout)//&& (st[pivot].h + projet.param_affectation_horaire.tboa < projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta*1440f))
                                                        {
                                                            int ii, jj, num_service = -1, h3 = -1, duree_periode, delta;
                                                            float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[successeur].services.Count; ii++)
                                                            {
                                                                delta = 0;
                                                                //st_delta[successeur][ii] = 0;
                                                                duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[ii].regime].Length;

                                                                if ((projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f < st[pivot].h + temps_correspondance) || projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[ii].regime].Substring(jour, 1) == "N")
                                                                {

                                                                    h1 = 1e38f;
                                                                    h2 = 1e38f;
                                                                    h3 = -1;
                                                                    for (jj = jour + 1; jj <= Math.Min(jour + projet.param_affectation_horaire.nb_jours, duree_periode - 1); jj++)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[successeur].services[ii].regime].Substring(jj, 1) == "O" && (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + (-jour + jj) * 24f * 60f) < h1 && (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + (-jour + jj) * 24f * 60f - temps_correspondance) > st[pivot].h)
                                                                        {
                                                                            h1 = projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + (-jour + jj) * 24f * 60f;
                                                                            h2 = (-jour + jj);
                                                                            h3 = jj;
                                                                        }

                                                                    }
                                                                    if (h3 != -1)
                                                                    {
                                                                        if (st_delta[successeur][ii] < h2 || st[successeur].touche == 0)
                                                                        {
                                                                            st_delta[successeur][ii] = h2;
                                                                        }


                                                                    }
                                                                    else
                                                                    {
                                                                        delta = -1;
                                                                    }


                                                                }
                                                                if ((projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f < st[pivot].h + max_correspondance) && (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f >= st[pivot].h + temps_correspondance))
                                                                {
                                                                    if (st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f - st[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[succ_type]) + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type] < cout2 && delta > -1)
                                                                    {
                                                                        cout2 = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[ii].hd + st_delta[successeur][ii] * 1440f - st[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[succ_type]) + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                                        num_service = ii;
                                                                    }
                                                                }

                                                            }
                                                            if (num_service != -1)
                                                            {
                                                                if (st[successeur].cout > st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd + st_delta[successeur][num_service] * 1440f - st[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[succ_type]) + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type])
                                                                {
                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Remove(successeur);
                                                                    st[successeur].service = num_service;
                                                                    st[successeur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd) * projet.param_affectation_horaire.cveh[succ_type] + (projet.reseaux[projet.reseau_actif].links[successeur].services[num_service].hd + st_delta[successeur][num_service] * 1440f - st[pivot].h) * projet.param_affectation_horaire.cwait[succ_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[succ_type]) + projet.reseaux[projet.reseau_actif].links[successeur].toll * projet.param_affectation_horaire.ctoll[succ_type];
                                                                    st[successeur].touche = 2;

                                                                    st[successeur].h = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f;
                                                                    if (st[pivot].ncorr == 0)
                                                                    {
                                                                        st[successeur].tatt1 = projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f - st[pivot].h;
                                                                    }
                                                                    else
                                                                    {
                                                                        st[successeur].tatt1 = st[pivot].tatt1;
                                                                    }
                                                                    st[successeur].tatt = st[pivot].tatt + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].delta * 1440f - st[pivot].h;
                                                                    st[successeur].tveh = st[pivot].tveh + projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hf - projet.reseaux[projet.reseau_actif].links[successeur].services[st[successeur].service].hd;
                                                                    st[successeur].tcor = st[pivot].tcor + temps_correspondance;
                                                                    st[successeur].ncorr = st[pivot].ncorr + 1;
                                                                    st[successeur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[successeur].longueur;
                                                                    st[successeur].tmap = st[pivot].tmap;
                                                                    st[successeur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[successeur].toll;

                                                                    st[successeur].pivot = pivot;
                                                                    st[successeur].turn_pivot = j;
                                                                    if (st[pivot].pole == depart)
                                                                    {
                                                                        st[successeur].pole = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;
                                                                    }
                                                                    else
                                                                    {
                                                                        st[successeur].pole = st[pivot].pole;
                                                                    }
                                                                    if (projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                                    {

                                                                        st[successeur].poleV2 = st[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;
                                                                    }
                                                                    else
                                                                    {

                                                                        st[successeur].poleV2 = st[pivot].poleV2 + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[successeur].no].i;
                                                                    }


                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[successeur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Add(successeur);
                                                                    nb_pop_local++;
                                                                }
                                                            }
                                                        }


                                                    }
                                                }
                                            }
                                            //st[pivot].touche = 3;
                                            //Console.WriteLine((touches.Count+calcules.Count).ToString());
                                        }
                                    fin_gga:
                                        //Console.WriteLine(p.ToString());

                                        int arrivee = -1;
                                        double cout_fin = 1e38f;
                                        if (projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(q, out value) == true)
                                        {
                                            for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[q]].pred.Count; j++)
                                            {
                                                int predecesseur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[q]].pred[j];
                                                if (st[predecesseur].touche != 0 && st[predecesseur].cout < cout_fin)
                                                {
                                                    arrivee = predecesseur;
                                                    cout_fin = st[predecesseur].cout;

                                                }




                                            }
                                        }
                                        else
                                        {
                                            fich_log.WriteLine("OD error" + libod + ":" + chaine + ": non existing destination node!");
                                        }



                                        if (arrivee != -1)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].links[arrivee].ligne > 0)
                                            {
                                                st[arrivee].alij += od;
                                                svc_tmp[arrivee][st[arrivee].service].alij = od;
                                                svc_tmp[arrivee][st[arrivee].service].alit += od;
                                            }
                                        }
                                        else
                                        {
                                            fich_log.WriteLine("OD error" + libod + ":" + chaine + ": unreachable destination!");
                                        }

                                        pivot = arrivee;
                                        string itineraire = "", texte;
                                        if (pivot != -1)
                                        {
                                            string[] param2 = { "|" }, lignes_corr = null;
                                            if (projet.reseaux[projet.reseau_actif].links[pivot].texte != null)
                                            {
                                                lignes_corr = projet.reseaux[projet.reseau_actif].links[pivot].texte.Split(param2, StringSplitOptions.RemoveEmptyEntries);
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
                                            st[pivot].volau += od;
                                            if (st[pivot].pivot != -1 && projet.param_affectation_horaire.sortie_turns == true)
                                            {
                                                Turn virage = new Turn();
                                                virage.arci = st[pivot].pivot;
                                                virage.arcj = pivot;
                                                float value2;
                                                if (transfers.TryGetValue(virage, out value2) == true)
                                                {

                                                    transfers[virage] += od;
                                                }
                                                else
                                                {
                                                    transfers[virage] = od;
                                                }

                                                //projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].arcj[st[pivot].turn_pivot].volau += od;
                                            }
                                            if (st[pivot].service >= 0)
                                            {
                                                svc_tmp[pivot][st[pivot].service].volau += od;
                                            }

                                            if (st[pivot].pivot == -1)
                                            {
                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                {
                                                    st[pivot].boai += od;
                                                    svc_tmp[pivot][st[pivot].service].boai = od;
                                                    svc_tmp[pivot][st[pivot].service].boat += od;

                                                }
                                            }
                                            else if (projet.reseaux[projet.reseau_actif].links[pivot].ligne != projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].ligne)
                                            {
                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                {
                                                    st[pivot].boai += od;
                                                    svc_tmp[pivot][st[pivot].service].boai = od;
                                                    svc_tmp[pivot][st[pivot].service].boat += od;
                                                }
                                                if (projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].ligne > 0)
                                                {
                                                    st[st[pivot].pivot].alij += od;
                                                    svc_tmp[st[pivot].pivot][st[st[pivot].pivot].service].alij = od;
                                                    svc_tmp[st[pivot].pivot][st[st[pivot].pivot].service].alit += od;
                                                }

                                            }
                                            if (projet.param_affectation_horaire.sortie_chemins == true)
                                            {
                                                texte = libod + ";" + p + ";" + q + ";" + jour.ToString("0") + ";" + horaire.ToString("0.000");

                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].no].i;
                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].nd].i;
                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].no].i + "-" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].nd].i;
                                                texte += ";" + projet.reseaux[projet.reseau_actif].links[pivot].ligne.ToString("0");
                                                if (st[pivot].service >= 0)
                                                {
                                                    texte += ";" + projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].numero.ToString("0");
                                                }
                                                else
                                                {
                                                    texte += ";-1";
                                                }
                                                texte += ";" + (st[pivot].h - horaire).ToString("0.000");
                                                texte += ";" + st[pivot].h.ToString("0.000");
                                                texte += ";" + st[pivot].tveh.ToString("0.000");
                                                texte += ";" + st[pivot].tmap.ToString("0.000");
                                                texte += ";" + st[pivot].tatt.ToString("0.000");
                                                texte += ";" + st[pivot].tcor.ToString("0.000");
                                                texte += ";" + st[pivot].ncorr.ToString("0");
                                                texte += ";" + st[pivot].tatt1.ToString("0.000");
                                                texte += ";" + st[pivot].cout.ToString("0.000");
                                                texte += ";" + st[pivot].l.ToString("0.000");
                                                texte += ";" + st[pivot].pole;
                                                texte += ";" + od.ToString("0.00");
                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                {
                                                    texte += ";" + svc_tmp[pivot][st[pivot].service].boai.ToString("0.000");
                                                    texte += ";" + svc_tmp[pivot][st[pivot].service].alij.ToString("0.000");
                                                    svc_tmp[pivot][st[pivot].service].boai = 0;
                                                    svc_tmp[pivot][st[pivot].service].alij = 0;

                                                }
                                                else
                                                {
                                                    texte += ";0.000";
                                                    texte += ";0.000";
                                                }

                                                texte += ";" + projet.reseaux[projet.reseau_actif].links[pivot].texte;
                                                texte += ";" + projet.reseaux[projet.reseau_actif].links[pivot].type;

                                                texte += ";" + st[pivot].ttoll.ToString("0.000");

                                                lignes_gr.chemins.Add(texte);


                                            }
                                            if (st[pivot].pivot != -1)
                                            {
                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne != projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].ligne)
                                                {
                                                    string[] param2 = { "|" }, lignes_corr = null;
                                                    if (projet.reseaux[projet.reseau_actif].links[pivot].texte != null)
                                                    {

                                                        lignes_corr = projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].texte.Split(param2, StringSplitOptions.RemoveEmptyEntries);
                                                    }
                                                    if (lignes_corr == null)
                                                    {
                                                        itineraire = "MAP|" + itineraire; ;
                                                    }
                                                    else
                                                    {
                                                        itineraire = lignes_corr[0] + "|" + itineraire;
                                                    }
                                                }
                                            }
                                            pivot = st[pivot].pivot;
                                        }
                                        //fich_sortie.WriteLine("o;i;j;jour;heureo;heured;temps;tveh;tmap;tcorr;cout;volau;texte" );
                                        if (arrivee != -1)
                                        {
                                            texte = libod + ";" + p + ";" + q;
                                            texte += ";" + jour.ToString("0.000");
                                            texte += ";" + horaire.ToString("0.000");
                                            texte += ";" + st[arrivee].h.ToString("0.000");
                                            texte += ";" + (-horaire + st[arrivee].h).ToString("0.000");
                                            texte += ";" + st[arrivee].tveh.ToString("0.000");
                                            texte += ";" + st[arrivee].tmap.ToString("0.000");
                                            texte += ";" + st[arrivee].tatt.ToString("0.000");
                                            texte += ";" + st[arrivee].tcor.ToString("0.000");
                                            texte += ";" + st[arrivee].ncorr.ToString("0");
                                            texte += ";" + st[arrivee].tatt1.ToString("0.000");
                                            texte += ";" + st[arrivee].cout.ToString("0.000");
                                            texte += ";" + st[arrivee].l.ToString("0.000");
                                            texte += ";" + st[arrivee].pole;
                                            texte += ";" + od.ToString("0.00");
                                            //                                texte += ";" + projet.reseaux[projet.reseau_actif].links[arrivee].texte;
                                            //itineraire = "MAP," + itineraire;

                                            texte += ";" + itineraire;
                                            texte += ";" + projet.param_affectation_horaire.nb_pop;
                                            texte += ";" + st[arrivee].ttoll.ToString("0.000");


                                            lignes_gr.od_line = texte;

                                            if (projet.param_affectation_horaire.sortie_noeuds == true)
                                            {
                                                foreach (node n in projet.reseaux[projet.reseau_actif].nodes)
                                                {
                                                    float tmax = 1e38f;
                                                    int which_tmax = -1;
                                                    String type_arc = "";
                                                    link troncon = new link();
                                                    for (int s = 0; s < n.pred.Count; s++)
                                                    {
                                                        troncon = projet.reseaux[projet.reseau_actif].links[n.pred[s]];
                                                        if (troncon.cout <= tmax && troncon.touche != 0 && (troncon.ligne <= 0 || projet.param_affectation_horaire.sortie_temps == 2))
                                                        {
                                                            tmax = troncon.cout;
                                                            which_tmax = n.pred[s];
                                                            type_arc = troncon.type;
                                                        }

                                                    }
                                                    if (which_tmax >= 0 && tmax <= projet.param_affectation_horaire.temps_max)
                                                    {
                                                        if (filtre.Contains(type_arc) || filtre.Count == 0)
                                                        {
                                                            if (projet.param_affectation_horaire.sortie_temps == 3)
                                                            {
                                                                texte = p;
                                                                texte += ";" + n.i;
                                                                texte += ";" + (-horaire + st[which_tmax].h).ToString("0.000");
                                                                texte += ";" + st[which_tmax].tatt1.ToString("0.000");
                                                                texte += ";" + od.ToString("0.000");

                                                            }
                                                            else
                                                            {
                                                                texte = libod + ";" + p + ";" + q;
                                                                texte += ";" + jour.ToString("0.000");
                                                                texte += ";" + n.i;
                                                                texte += ";" + horaire.ToString("0.000");
                                                                texte += ";" + st[which_tmax].h.ToString("0.000");
                                                                texte += ";" + (-horaire + st[which_tmax].h).ToString("0.000");
                                                                texte += ";" + st[which_tmax].tveh.ToString("0.000");
                                                                texte += ";" + st[which_tmax].tmap.ToString("0.000");
                                                                texte += ";" + st[which_tmax].tatt.ToString("0.000");
                                                                texte += ";" + st[which_tmax].tcor.ToString("0.000");
                                                                texte += ";" + st[which_tmax].ncorr.ToString("0");
                                                                texte += ";" + st[which_tmax].tatt1.ToString("0.000");
                                                                texte += ";" + st[which_tmax].cout.ToString("0.000");
                                                                texte += ";" + st[which_tmax].l.ToString("0.000");
                                                                texte += ";" + st[which_tmax].pole;
                                                                texte += ";" + st[which_tmax].ttoll.ToString("0.000");
                                                                texte += ";" + od.ToString("0.000");
                                                                if (projet.param_affectation_horaire.sortie_stops == true)
                                                                {
                                                                    texte += ";" + st[which_tmax].poleV2;
                                                                }
                                                                else
                                                                {
                                                                    texte += ";";
                                                                }
                                                            }
                                                            lignes_gr.noeuds.Add(texte);
                                                        }
                                                    }
                                                }
                                            }

                                            if (projet.param_affectation_horaire.sortie_temps == 0)
                                            {
                                                network reseau = projet.reseaux[projet.reseau_actif];
                                                double som_detour = 0; double nb_detour = 0; double som_oiseau = 0;
                                                double d_oiseau, d_link;
                                                foreach (link li in reseau.links)
                                                {
                                                    d_oiseau = Math.Pow(Math.Pow((reseau.nodes[reseau.numnoeud[p]].x - reseau.nodes[li.nd].x), 2) + Math.Pow((reseau.nodes[reseau.numnoeud[p]].y - reseau.nodes[li.nd].y), 2), 0.5);
                                                    d_link = Math.Pow(Math.Pow((reseau.nodes[li.no].x - reseau.nodes[li.nd].x), 2) + Math.Pow((reseau.nodes[li.no].y - reseau.nodes[li.nd].y), 2), 0.5);

                                                    if (d_oiseau > 500 & d_oiseau - d_link <= 500)
                                                    {

                                                        if (reseau.nodes[reseau.numnoeud[p]].x > 0 && reseau.nodes[reseau.numnoeud[p]].y > 0 && reseau.nodes[li.nd].x > 0 && reseau.nodes[li.nd].y > 0)
                                                        {
                                                            if (li.h > 0)
                                                            {
                                                                som_detour += li.h;
                                                                som_oiseau += Math.Pow(Math.Pow((reseau.nodes[reseau.numnoeud[p]].x - reseau.nodes[li.nd].x), 2) + Math.Pow((reseau.nodes[reseau.numnoeud[p]].y - reseau.nodes[li.nd].y), 2), 0.5);
                                                                nb_detour++;
                                                            }
                                                        }
                                                    }
                                                }
                                                lignes_gr.detour_line = p + ";" + som_detour.ToString() + ";" + som_oiseau.ToString() + ";" + nb_detour.ToString();

                                            }

                                            if (projet.param_affectation_horaire.sortie_temps > 0 && projet.param_affectation_horaire.sortie_temps < 4)
                                            {
                                                for (i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
                                                {
                                                    arrivee = i;


                                                    if (filtre.Contains(projet.reseaux[projet.reseau_actif].links[arrivee].type) || filtre.Count == 0)
                                                    {
                                                        if (st[arrivee].touche != 0 && (projet.reseaux[projet.reseau_actif].links[arrivee].ligne < 0 || projet.param_affectation_horaire.sortie_temps == 2))
                                                        {
                                                            if (projet.param_affectation_horaire.sortie_noeuds == false && projet.param_affectation_horaire.sortie_temps == 3)
                                                            {
                                                                texte = p;
                                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].no].i;
                                                                texte += "-" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].nd].i;
                                                                texte += ";" + (-horaire + st[arrivee].h).ToString("0.000");
                                                                texte += ";" + st[arrivee].tatt1.ToString("0.000");
                                                                texte += ";" + od.ToString("0.000");


                                                            }
                                                            else if (projet.param_affectation_horaire.sortie_temps < 3)
                                                            {
                                                                texte = libod + ";" + p;
                                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].no].i;
                                                                texte += "-" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].nd].i;
                                                                texte += ";" + (projet.reseaux[projet.reseau_actif].links[arrivee].ligne).ToString("0");
                                                                texte += ";" + i.ToString("0");
                                                                texte += ";" + jour.ToString("0");
                                                                texte += ";" + horaire.ToString("0.000");
                                                                texte += ";" + st[arrivee].h.ToString("0.000");
                                                                texte += ";" + (-horaire + st[arrivee].h).ToString("0.000");
                                                                texte += ";" + st[arrivee].tveh.ToString("0.000");
                                                                texte += ";" + st[arrivee].tmap.ToString("0.000");
                                                                texte += ";" + st[arrivee].tatt.ToString("0.000");
                                                                texte += ";" + st[arrivee].tcor.ToString("0.000");
                                                                texte += ";" + st[arrivee].ncorr.ToString("0");
                                                                texte += ";" + st[arrivee].tatt1.ToString("0.000");
                                                                texte += ";" + st[arrivee].cout.ToString("0.000");
                                                                texte += ";" + st[arrivee].l.ToString("0.000");
                                                                texte += ";" + st[arrivee].pole;
                                                                texte += ";" + od.ToString("0.00");
                                                                // texte += ";" + projet.reseaux[projet.reseau_actif].links[arrivee].texte;
                                                                /*texte += ";" + projet.param_affectation_horaire.texte_cveh;
                                                                texte += ";" + projet.param_affectation_horaire.texte_cwait;
                                                                texte += ";" + projet.param_affectation_horaire.texte_cmap;
                                                                texte += ";" + projet.param_affectation_horaire.texte_cboa;
                                                                texte += ";" + projet.param_affectation_horaire.texte_coef_tmap;
                                                                texte += ";" + projet.param_affectation_horaire.texte_tboa;
                                                                texte += ";" + projet.param_affectation_horaire.nb_jours;*/
                                                                texte += ";" + st[arrivee].pivot.ToString("0");
                                                                texte += ";" + projet.reseaux[projet.reseau_actif].links[arrivee].type;
                                                                texte += ";" + st[arrivee].ttoll.ToString("0.000");

                                                                link arc = new link();
                                                                arc = projet.reseaux[projet.reseau_actif].links[arrivee];
                                                                float ti;
                                                                if (arc.ligne < 0)
                                                                {
                                                                    ti = -horaire + (arc.h - arc.temps * projet.param_affectation_horaire.coef_tmap[arc.type]);

                                                                    //ti = -horaire + arc.h - arc.temps;
                                                                }
                                                                else
                                                                {
                                                                    ti = -horaire + arc.h - (arc.services[arc.service].hf - arc.services[arc.service].hd);
                                                                }
                                                                texte += ";" + ti.ToString("0.000");

                                                            }
                                                            //                                itineraire = "MAP," + itineraire;
                                                            //texte += ";" + itineraire;
                                                            if ((st[arrivee].cout) <= projet.param_affectation_horaire.temps_max)
                                                            {

                                                                if (test_temps_per_min_depart(projet, arrivee) == true)
                                                                {
                                                                    if (!(projet.param_affectation_horaire.sortie_temps == 3 && projet.param_affectation_horaire.sortie_noeuds == true))
                                                                    {
                                                                        lignes_gr.temps.Add(texte);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else if (st[arrivee].touche == 0 && projet.param_affectation_horaire.sortie_isoles == true)
                                                        {
                                                            texte = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].no].i;
                                                            texte += "-" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].nd].i;
                                                            texte += ";" + (projet.reseaux[projet.reseau_actif].links[arrivee].ligne).ToString("0");
                                                            texte += ";" + i.ToString("0");
                                                            lignes_gr.isoles.Add(texte);

                                                        }

                                                    }
                                                }
                                            }
                                        }
                                    }

                                    // sens heure d'arrivée
                                    // sens heure d'arrivée
                                    // sens heure d'arrivée
                                    // sens heure d'arrivée
                                    // sens heure d'arrivée
                                    // sens heure d'arrivée
                                    // sens heure d'arrivée
                                    if (sens == 2)
                                    {
                                        if (q1_loc == q && jour1_loc == jour && horaire1_loc == horaire && sens1_loc == sens && ch.Length < 13)
                                        {
                                            p1_loc = p;
                                            goto fin_gga2;
                                        }
                                        p1_loc = p; q1_loc = q; jour1_loc = jour; horaire1_loc = horaire; sens1_loc = sens;

                                        for (i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
                                        {
                                            st[i].touche = 0;
                                            st[i].cout = 0;
                                            st[i].tatt = 0;
                                            st[i].tatt1 = 0;
                                            st[i].tcor = 0;
                                            st[i].ncorr = 0;
                                            st[i].tmap = 0;
                                            st[i].tveh = 0;
                                            st[i].h = 0;
                                            st[i].ttoll = 0;
                                            st[i].l = 0;
                                            for (j = 0; j < projet.reseaux[projet.reseau_actif].links[i].services.Count; j++)
                                            {
                                                st_delta[i][j] = 0;
                                            }
                                            st[i].pivot = -1;
                                            st[i].turn_pivot = -1;
                                            st[i].pole = "-1";
                                            st[i].poleV2 = "";
                                            st[i].service = -1;
                                            st[i].is_queue = false;



                                        }
                                        gga_nq.Clear();
                                        string depart = q;
                                        int pivot = -1, value;
                                        int bucket, predecesseur;
                                        id_bucket = 0;
                                        float penalite = 0, temps_correspondance, max_correspondance;
                                        if (projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(depart, out value) == true)
                                        {

                                            for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[depart]].pred.Count; j++)
                                            {
                                                predecesseur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[depart]].pred[j];
                                                String pred_type = projet.reseaux[projet.reseau_actif].links[predecesseur].type;
                                                max_correspondance = projet.param_affectation_horaire.tboa_max[pred_type];




                                                if (projet.reseaux[projet.reseau_actif].links[predecesseur].ligne < 0 && projet.param_affectation_horaire.cmap[pred_type] > 0 && projet.reseaux[projet.reseau_actif].links[predecesseur].temps < projet.param_affectation_horaire.tmapmax)
                                                {
                                                    bool test_periode = false;

                                                    if (projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count > 0)
                                                    {
                                                        int decal_jour = (int)Math.Floor(horaire / 1440f);
                                                        for (int kk = 0; kk < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; kk++)
                                                        {
                                                            if (Math.Abs(decal_jour) <= projet.param_affectation_horaire.nb_jours)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hd + 1440f * decal_jour <= horaire && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hf + 1440f * decal_jour > horaire)
                                                                {
                                                                    test_periode = true;
                                                                    st[predecesseur].service = kk;
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
                                                        st[predecesseur].touche = 1;
                                                        st[predecesseur].cout = (projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type] * projet.param_affectation_horaire.cmap[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                        st[predecesseur].tmap = (projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type];
                                                        st[predecesseur].ttoll = projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                        st[predecesseur].h = horaire - (projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type];
                                                        st[predecesseur].l = projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                        st[predecesseur].pivot = -1;
                                                        st[predecesseur].turn_pivot = -1;

                                                        st[predecesseur].pole = depart;
                                                        st[predecesseur].poleV2 = "";
                                                        //                                    bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                        while (bucket >= gga_nq.Count)
                                                        {
                                                            gga_nq.Add(new List<int>());
                                                        }
                                                        gga_nq[bucket].Add(predecesseur);
                                                        nb_pop_local++;
                                                    }
                                                }
                                                else if (projet.param_affectation_horaire.cveh[pred_type] > 0)
                                                {
                                                    int ii, jj, num_service = -1, h3 = 0, delta, duree_periode;
                                                    float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                                    for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; ii++)
                                                    {
                                                        delta = 0;
                                                        duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Length;

                                                        if ((projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf > horaire) || projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                                                        {

                                                            h1 = -1e38f;
                                                            h2 = 1e38f;
                                                            h3 = -1;
                                                            for (jj = jour - 1; jj >= Math.Max(jour - projet.param_affectation_horaire.nb_jours, 0); jj--)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) < horaire)
                                                                {
                                                                    h1 = projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                                                    h2 = (-jour + jj);
                                                                    h3 = jj;
                                                                }

                                                            }
                                                            if (h3 != -1)
                                                            {
                                                                st_delta[predecesseur][ii] = h2;
                                                            }
                                                            else
                                                            {
                                                                delta = 1;
                                                            }


                                                        }

                                                        if (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + horaire < max_correspondance)
                                                        {
                                                            if (((projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + horaire) * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type]) < cout2 && delta < 1)
                                                            {
                                                                cout2 = (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + horaire) * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                num_service = ii;

                                                            }
                                                        }

                                                    }
                                                    if (num_service != -1)
                                                    {
                                                        st[predecesseur].service = num_service;
                                                        st[predecesseur].cout = (projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - st_delta[predecesseur][num_service] * 1440f + horaire) * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];

                                                        st[predecesseur].touche = 1;

                                                        st[predecesseur].h = projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 60f * 24f;
                                                        st[predecesseur].tatt = -projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + horaire;
                                                        st[predecesseur].tatt1 = -projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + horaire;
                                                        st[predecesseur].tveh = projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd;
                                                        st[predecesseur].tcor = 0;
                                                        st[predecesseur].ncorr = 1;
                                                        st[predecesseur].l = projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                        st[predecesseur].tmap = 0;
                                                        st[predecesseur].ttoll = projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                        while (bucket >= gga_nq.Count)
                                                        {
                                                            gga_nq.Add(new List<int>());
                                                        }
                                                        gga_nq[bucket].Add(predecesseur);
                                                        nb_pop_local++;
                                                        //                                touches.Enqueue(successeur);
                                                        st[predecesseur].pivot = -1;
                                                        st[predecesseur].turn_pivot = -1;
                                                        st[predecesseur].pole = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i;
                                                        st[predecesseur].poleV2 = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i;
                                                    }
                                                }

                                            }
                                        }
                                        else
                                        {
                                            id_bucket++;
                                            fich_log.WriteLine("OD error" + libod + ":" + chaine + ": non existing destination node!");
                                        }
                                        int bucket_cout_max = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(projet.param_affectation_horaire.temps_max / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));

                                        while (gga_nq.Count >= id_bucket && bucket_cout_max >= id_bucket)
                                        {

                                            while (gga_nq[id_bucket].Count == 0)
                                            {
                                                id_bucket++;
                                                if (id_bucket >= gga_nq.Count || id_bucket >= bucket_cout_max + 1)
                                                {
                                                    goto fin_gga2;
                                                }
                                            }
                                            if (projet.param_affectation_horaire.algorithme == 0)
                                            {
                                                pivot = gga_nq[id_bucket][0];
                                                gga_nq[id_bucket].RemoveAt(0);

                                            }
                                            else
                                            {
                                                int k, id_pivot = -1; double cout_max = 1e38f;
                                                for (k = 0; k <= gga_nq[id_bucket].Count; k++)
                                                {
                                                    if (st[gga_nq[id_bucket][k]].cout < cout_max)
                                                    {
                                                        cout_max = st[gga_nq[id_bucket][k]].cout;
                                                        id_pivot = k;
                                                    }
                                                }
                                                pivot = gga_nq[id_bucket][id_pivot];
                                                gga_nq[id_bucket].RemoveAt(id_pivot);
                                                st[pivot].touche = 3;
                                            }


                                            //avancement.textBox1.Text = touches.Count.ToString() + " " + calcules.Count.ToString() + " " + st[pivot].cout;
                                            //avancement.textBox1.Refresh();
                                            for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].no].pred.Count; j++)
                                            {

                                                String pivot_type = projet.reseaux[projet.reseau_actif].links[pivot].type;
                                                link troncon_pred = projet.reseaux[projet.reseau_actif].links[projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].no].pred[j]];
                                                link troncon_pivot = projet.reseaux[projet.reseau_actif].links[pivot];
                                                predecesseur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].no].pred[j];
                                                String pred_type = projet.reseaux[projet.reseau_actif].links[predecesseur].type;

                                                if (projet.param_affectation_horaire.demitours == true)
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
                                                        pred_type = projet.reseaux[projet.reseau_actif].links[predecesseur].type;
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
                                                        temps_correspondance = penalite + projet.param_affectation_horaire.tboa[pivot_type];
                                                        max_correspondance = projet.param_affectation_horaire.tboa_max[pivot_type];

                                                    }
                                                    else
                                                    {
                                                        temps_correspondance = projet.param_affectation_horaire.tboa[pivot_type];
                                                        max_correspondance = projet.param_affectation_horaire.tboa_max[pivot_type];

                                                    }
                                                    //successeurs touches pour la première fois
                                                    if (st[predecesseur].touche == 0)
                                                    {
                                                        // predecesseur marche à pied pivot marche
                                                        if (projet.reseaux[projet.reseau_actif].links[predecesseur].ligne < 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne < 0 && projet.param_affectation_horaire.cmap[pred_type] > 0 && (st[pivot].tmap + projet.reseaux[projet.reseau_actif].links[predecesseur].temps < projet.param_affectation_horaire.tmapmax))
                                                        {
                                                            bool test_periode = false;
                                                            st[predecesseur].service = -1;
                                                            if (projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count > 0)
                                                            {
                                                                int decal_jour = (int)(Math.Floor((st[pivot].h - penalite) / 1440f));
                                                                for (int kk = 0; kk < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; kk++)
                                                                {
                                                                    if (Math.Abs(decal_jour) <= projet.param_affectation_horaire.nb_jours)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hd + 1440f * decal_jour <= st[pivot].h - penalite && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hf + 1440f * decal_jour > st[pivot].h - penalite)
                                                                        {
                                                                            test_periode = true;
                                                                            st[predecesseur].service = kk;
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

                                                                st[predecesseur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].temps + penalite) * projet.param_affectation_horaire.coef_tmap[pred_type] * projet.param_affectation_horaire.cmap[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                st[predecesseur].h = st[pivot].h - (projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type] - penalite;
                                                                st[predecesseur].tatt = st[pivot].tatt;
                                                                st[predecesseur].tatt1 = st[pivot].tatt1;
                                                                st[predecesseur].tveh = st[pivot].tveh;
                                                                st[predecesseur].tcor = st[pivot].tcor;
                                                                st[predecesseur].ncorr = st[pivot].ncorr;
                                                                st[predecesseur].tmap = st[pivot].tmap + (projet.reseaux[projet.reseau_actif].links[predecesseur].temps + penalite) * projet.param_affectation_horaire.coef_tmap[pred_type];
                                                                st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                                st[predecesseur].touche = 1;

                                                                //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                while (bucket >= gga_nq.Count)
                                                                {
                                                                    gga_nq.Add(new List<int>());
                                                                }
                                                                gga_nq[bucket].Add(predecesseur);
                                                                nb_pop_local++;
                                                                //                                        touches.Enqueue(successeur);
                                                                st[predecesseur].pivot = pivot;
                                                                st[predecesseur].turn_pivot = j;
                                                                st[predecesseur].pole = st[pivot].pole;
                                                                st[predecesseur].poleV2 = st[pivot].poleV2;
                                                            }
                                                        }
                                                        // predecesseur marche à pied pivot TC
                                                        else if (projet.reseaux[projet.reseau_actif].links[predecesseur].ligne < 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0 && projet.param_affectation_horaire.cmap[pred_type] > 0 && (st[pivot].tmap + projet.reseaux[projet.reseau_actif].links[predecesseur].temps < projet.param_affectation_horaire.tmapmax))
                                                        {
                                                            bool test_periode = false;
                                                            st[predecesseur].service = -1;


                                                            if (projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count > 0)
                                                            {
                                                                int decal_jour = -(int)(Math.Floor((st[pivot].h - temps_correspondance) / 1440f));
                                                                for (int kk = 0; kk < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; kk++)
                                                                {
                                                                    if (decal_jour <= projet.param_affectation_horaire.nb_jours)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].regime].Substring(jour - decal_jour, 1) == "O" && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hd - 1440f * decal_jour <= st[pivot].h - temps_correspondance && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hf - 1440f * decal_jour > st[pivot].h - temps_correspondance)
                                                                        {
                                                                            test_periode = true;
                                                                            st[predecesseur].service = kk;
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


                                                                st[predecesseur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].temps + penalite) * projet.param_affectation_horaire.coef_tmap[pred_type] * projet.param_affectation_horaire.cmap[pred_type] + projet.param_affectation_horaire.cboa[pivot_type] * temps_correspondance + temps_correspondance * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                st[predecesseur].h = st[pivot].h - (projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type] - temps_correspondance - penalite;
                                                                st[predecesseur].tatt = st[pivot].tatt + temps_correspondance;
                                                                st[predecesseur].tatt1 = st[pivot].tatt1;
                                                                st[predecesseur].tveh = st[pivot].tveh;
                                                                st[predecesseur].tcor = st[pivot].tcor + temps_correspondance;
                                                                st[predecesseur].ncorr = st[pivot].ncorr + 1;
                                                                st[predecesseur].tmap = st[pivot].tmap + (projet.reseaux[projet.reseau_actif].links[predecesseur].temps + penalite) * projet.param_affectation_horaire.coef_tmap[pred_type];
                                                                st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                                st[predecesseur].touche = 1;
                                                                // bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                while (bucket >= gga_nq.Count)
                                                                {
                                                                    gga_nq.Add(new List<int>());
                                                                }
                                                                gga_nq[bucket].Add(predecesseur);
                                                                nb_pop_local++;
                                                                //                                        touches.Enqueue(successeur);
                                                                st[predecesseur].pivot = pivot;
                                                                st[predecesseur].turn_pivot = j;
                                                                st[predecesseur].pole = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i;
                                                                st[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i + st[pivot].poleV2;
                                                            }
                                                        }
                                                        //predecesseurs TC même ligne
                                                        else if (projet.reseaux[projet.reseau_actif].links[predecesseur].ligne == projet.reseaux[projet.reseau_actif].links[pivot].ligne && projet.reseaux[projet.reseau_actif].links[predecesseur].ligne > 0 && projet.param_affectation_horaire.cveh[pred_type] > 0)
                                                        {
                                                            int ii, num_service = -1;
                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; ii++)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].numero == projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].numero)
                                                                {
                                                                    if (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf <= projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].hd)
                                                                    {
                                                                        num_service = ii;
                                                                    }
                                                                }
                                                            }
                                                            if (num_service != -1 && projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf + projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta * 1440f <= st[pivot].h)
                                                            {
                                                                st[predecesseur].service = num_service;
                                                                st_delta[predecesseur][num_service] = projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta;

                                                                st[predecesseur].touche = 1;
                                                                st[predecesseur].cout = st[pivot].cout + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h) * projet.param_affectation_horaire.cveh[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                st[predecesseur].h = projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 60f * 24f;
                                                                st[predecesseur].tatt = st[pivot].tatt;
                                                                st[predecesseur].tatt1 = st[pivot].tatt1;
                                                                st[predecesseur].tveh = st[pivot].tveh - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h;
                                                                st[predecesseur].tcor = st[pivot].tcor;
                                                                st[predecesseur].ncorr = st[pivot].ncorr;
                                                                st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                                st[predecesseur].tmap = st[pivot].tmap;
                                                                st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                while (bucket >= gga_nq.Count)
                                                                {
                                                                    gga_nq.Add(new List<int>());
                                                                }
                                                                gga_nq[bucket].Add(predecesseur);
                                                                nb_pop_local++;
                                                                //touches.Enqueue(successeur);
                                                                st[predecesseur].pivot = pivot;
                                                                st[predecesseur].turn_pivot = j;
                                                                st[predecesseur].pole = st[pivot].pole;
                                                                st[predecesseur].poleV2 = st[pivot].poleV2;
                                                            }
                                                        }

                                                        //predecesseur TC lignes différentes
                                                        else if (projet.reseaux[projet.reseau_actif].links[predecesseur].ligne != projet.reseaux[projet.reseau_actif].links[pivot].ligne && projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0 && projet.reseaux[projet.reseau_actif].links[predecesseur].ligne > 0 && projet.param_affectation_horaire.cveh[pred_type] > 0)
                                                        {
                                                            int ii, jj, num_service = -1, h3 = 0, delta, duree_periode;
                                                            float h1 = -1e38f, h2 = 1e38f, cout2 = 1e38f;
                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; ii++)
                                                            {
                                                                delta = 0;
                                                                duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Length;

                                                                if ((projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + temps_correspondance > st[pivot].h) || projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                                                                {

                                                                    h1 = -1e38f;
                                                                    h2 = 1e38f;
                                                                    h3 = -1;
                                                                    for (jj = jour - 1; jj >= Math.Max(jour - projet.param_affectation_horaire.nb_jours, 0); jj--)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) + temps_correspondance < st[pivot].h)
                                                                        {
                                                                            h1 = projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                                                            h2 = (-jour + jj);
                                                                            h3 = jj;
                                                                        }

                                                                    }
                                                                    if (h3 != -1)
                                                                    {
                                                                        if (st_delta[predecesseur][ii] > h2 || st[predecesseur].touche == 0)
                                                                        {
                                                                            st_delta[predecesseur][ii] = h2;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        delta = 1;
                                                                    }


                                                                }
                                                                if ((projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + st_delta[predecesseur][ii] * 1440f + max_correspondance > st[pivot].h) && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + st_delta[predecesseur][ii] * 1440f + temps_correspondance <= st[pivot].h))
                                                                {
                                                                    if ((st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + temps_correspondance * projet.param_affectation_horaire.cboa[pivot_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type]) < cout2 && delta < 1)
                                                                    {

                                                                        cout2 = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + temps_correspondance * projet.param_affectation_horaire.cboa[pivot_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                        num_service = ii;

                                                                    }
                                                                }

                                                            }
                                                            if (num_service != -1)
                                                            {
                                                                st[predecesseur].service = num_service;
                                                                st[predecesseur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - st_delta[predecesseur][num_service] * 1440f + st[pivot].h) * projet.param_affectation_horaire.cwait[pivot_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[pivot_type]) + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];

                                                                st[predecesseur].touche = 1;

                                                                st[predecesseur].h = projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 60f * 24f;
                                                                if (st[pivot].ncorr == 0)
                                                                {
                                                                    st[predecesseur].tatt1 = -projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h;
                                                                }
                                                                else
                                                                {
                                                                    st[predecesseur].tatt1 = st[pivot].tatt1;
                                                                }

                                                                st[predecesseur].tatt = st[pivot].tatt - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h;
                                                                st[predecesseur].tveh = st[pivot].tveh + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd;
                                                                st[predecesseur].tcor = st[pivot].tcor + temps_correspondance;
                                                                st[predecesseur].ncorr = st[pivot].ncorr + 1;
                                                                st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                                st[predecesseur].tmap = st[pivot].tmap;
                                                                st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                while (bucket >= gga_nq.Count)
                                                                {
                                                                    gga_nq.Add(new List<int>());
                                                                }
                                                                gga_nq[bucket].Add(predecesseur);
                                                                nb_pop_local++;
                                                                //                                        touches.Enqueue(successeur);
                                                                st[predecesseur].pivot = pivot;
                                                                st[predecesseur].turn_pivot = j;
                                                                st[predecesseur].pole = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i;
                                                                st[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i + st[pivot].poleV2;
                                                            }
                                                        }

                                                        //predecesseur TC lignes différentes pivot MAP
                                                        else if (projet.reseaux[projet.reseau_actif].links[predecesseur].ligne > 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne < 0 && projet.param_affectation_horaire.cveh[pred_type] > 0)
                                                        {
                                                            int ii, jj, num_service = -1, h3 = 0, delta, duree_periode;
                                                            float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; ii++)
                                                            {
                                                                delta = 0;
                                                                duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Length;
                                                                if ((projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf > st[pivot].h) || projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                                                                {
                                                                    h1 = -1e38f;
                                                                    h2 = 1e38f;
                                                                    h3 = -1;

                                                                    for (jj = jour - 1; jj >= Math.Max(jour - projet.param_affectation_horaire.nb_jours, 0); jj--)
                                                                    {

                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) < st[pivot].h)
                                                                        {
                                                                            h1 = projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                                                            h2 = (-jour + jj);
                                                                            h3 = jj;
                                                                        }

                                                                    }
                                                                    if (h3 != -1)
                                                                    {
                                                                        if (st_delta[predecesseur][ii] > h2 || st[predecesseur].touche == 0)
                                                                        {
                                                                            st_delta[predecesseur][ii] = h2;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        delta = 1;
                                                                    }


                                                                }
                                                                if ((projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + st_delta[predecesseur][ii] * 1440f <= st[pivot].h))

                                                                {
                                                                    if ((st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type]) + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type] < cout2 && delta < 1)
                                                                    {
                                                                        cout2 = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                        num_service = ii;

                                                                    }
                                                                }

                                                            }
                                                            if (num_service != -1)
                                                            {
                                                                st[predecesseur].service = num_service;
                                                                st[predecesseur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - st_delta[predecesseur][num_service] * 1440f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];

                                                                st[predecesseur].touche = 1;

                                                                st[predecesseur].h = projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 24f * 60f;
                                                                if (st[pivot].ncorr == 0)
                                                                {
                                                                    st[predecesseur].tatt1 = -projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h;
                                                                }
                                                                else
                                                                {
                                                                    st[predecesseur].tatt1 = st[pivot].tatt1;

                                                                }
                                                                st[predecesseur].tatt = st[pivot].tatt - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h;
                                                                st[predecesseur].tveh = st[pivot].tveh + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd;
                                                                st[predecesseur].tcor = st[pivot].tcor;
                                                                st[predecesseur].ncorr = st[pivot].ncorr;
                                                                st[predecesseur].tmap = st[pivot].tmap;
                                                                st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;                                                //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                while (bucket >= gga_nq.Count)
                                                                {
                                                                    gga_nq.Add(new List<int>());
                                                                }
                                                                gga_nq[bucket].Add(predecesseur);
                                                                nb_pop_local++;
                                                                //                                        touches.Enqueue(successeur);
                                                                st[predecesseur].pivot = pivot;
                                                                st[predecesseur].turn_pivot = j;
                                                                st[predecesseur].pole = st[pivot].pole;
                                                                st[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i + st[pivot].poleV2;
                                                            }
                                                        }

                                                    }


                                                    //eléments déjà touchés
                                                    else if (st[predecesseur].touche == 1 || st[predecesseur].touche == 2)
                                                    {
                                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));


                                                        //successeurs marche à pied pivot MAP

                                                        if (projet.reseaux[projet.reseau_actif].links[predecesseur].ligne < 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne < 0 && projet.param_affectation_horaire.cmap[pred_type] > 0 && (st[pivot].tmap + projet.reseaux[projet.reseau_actif].links[predecesseur].temps < projet.param_affectation_horaire.tmapmax) && st[predecesseur].cout > st[pivot].cout)
                                                        {
                                                            bool test_periode = false;
                                                            int id_service = -1;
                                                            if (projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count > 0)
                                                            {
                                                                int decal_jour = (int)(Math.Floor((st[pivot].h - penalite) / 1440f));
                                                                for (int kk = 0; kk < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; kk++)
                                                                {
                                                                    if (Math.Abs(decal_jour) <= projet.param_affectation_horaire.nb_jours)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].regime].Substring(jour + decal_jour, 1) == "O" && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hd + 1440f * decal_jour <= st[pivot].h - penalite && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hf + 1440f * decal_jour > st[pivot].h - penalite)
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


                                                                if (st[predecesseur].cout > st[pivot].cout + (penalite + projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type] * projet.param_affectation_horaire.cmap[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type])
                                                                {

                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Remove(predecesseur);
                                                                    st[predecesseur].cout = st[pivot].cout + (penalite + projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type] * projet.param_affectation_horaire.cmap[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                    st[predecesseur].h = st[pivot].h - (projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type] - penalite;
                                                                    st[predecesseur].tatt = st[pivot].tatt;
                                                                    st[predecesseur].tatt1 = st[pivot].tatt1;
                                                                    st[predecesseur].tveh = st[pivot].tveh;
                                                                    st[predecesseur].tcor = st[pivot].tcor;
                                                                    st[predecesseur].ncorr = st[pivot].ncorr;
                                                                    st[predecesseur].tmap = st[pivot].tmap + (penalite + projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type];
                                                                    st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                    st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                                    st[predecesseur].touche = 2;

                                                                    st[predecesseur].pivot = pivot;
                                                                    st[predecesseur].turn_pivot = j;
                                                                    st[predecesseur].pole = st[pivot].pole;
                                                                    st[predecesseur].poleV2 = st[pivot].poleV2;

                                                                    st[predecesseur].service = id_service;

                                                                    //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Add(predecesseur);
                                                                    nb_pop_local++;

                                                                }
                                                            }

                                                        }
                                                        //predecesseurs marche à pied pivot TC
                                                        else if (projet.reseaux[projet.reseau_actif].links[predecesseur].ligne < 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0 && projet.param_affectation_horaire.cmap[pred_type] > 0 && (st[pivot].tmap + projet.reseaux[projet.reseau_actif].links[predecesseur].temps < projet.param_affectation_horaire.tmapmax) && st[predecesseur].cout > st[pivot].cout)
                                                        {
                                                            int id_service = -1;
                                                            bool test_periode = false;

                                                            if (projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count > 0)
                                                            {
                                                                int decal_jour = -(int)(Math.Floor((st[pivot].h - temps_correspondance) / 1440f));
                                                                for (int kk = 0; kk < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; kk++)
                                                                {
                                                                    if (decal_jour <= projet.param_affectation_horaire.nb_jours)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].regime].Substring(jour - decal_jour, 1) == "O" && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hd - 1440f * decal_jour <= st[pivot].h - penalite - temps_correspondance && projet.reseaux[projet.reseau_actif].links[predecesseur].services[kk].hf - 1440f * decal_jour > st[pivot].h - penalite - temps_correspondance)
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


                                                                if (st[predecesseur].cout > st[pivot].cout + (penalite + projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type] * projet.param_affectation_horaire.cmap[pred_type] + projet.param_affectation_horaire.cboa[pivot_type] * temps_correspondance + projet.param_affectation_horaire.cwait[pred_type] * temps_correspondance + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type])
                                                                {
                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Remove(predecesseur);
                                                                    st[predecesseur].cout = st[pivot].cout + (penalite + projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type] * projet.param_affectation_horaire.cmap[pred_type] + projet.param_affectation_horaire.cboa[pivot_type] * temps_correspondance + projet.param_affectation_horaire.cwait[pred_type] * temps_correspondance + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                    st[predecesseur].h = st[pivot].h - (projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type] - temps_correspondance - penalite;
                                                                    st[predecesseur].tatt = st[pivot].tatt + temps_correspondance;
                                                                    st[predecesseur].tatt1 = st[pivot].tatt1;
                                                                    st[predecesseur].tveh = st[pivot].tveh;
                                                                    st[predecesseur].tcor = st[pivot].tcor + temps_correspondance;
                                                                    st[predecesseur].ncorr = st[pivot].ncorr + 1;
                                                                    st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                                    st[predecesseur].tmap = st[pivot].tmap + (penalite + projet.reseaux[projet.reseau_actif].links[predecesseur].temps) * projet.param_affectation_horaire.coef_tmap[pred_type];
                                                                    st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                    st[predecesseur].touche = 2;

                                                                    st[predecesseur].pivot = pivot;
                                                                    st[predecesseur].pole = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i;
                                                                    st[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i + st[pivot].poleV2;

                                                                    st[predecesseur].turn_pivot = j;

                                                                    st[predecesseur].service = id_service;
                                                                    //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Add(predecesseur);
                                                                    nb_pop_local++;

                                                                }
                                                            }

                                                        }
                                                        //successeurs TC même ligne
                                                        else if ((projet.reseaux[projet.reseau_actif].links[predecesseur].ligne == projet.reseaux[projet.reseau_actif].links[pivot].ligne && projet.param_affectation_horaire.cveh[pred_type] > 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0) && (st[predecesseur].cout > st[pivot].cout))
                                                        {
                                                            int ii, num_service = -1;
                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; ii++)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].numero == projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].numero)
                                                                {
                                                                    if (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf <= projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].hd)
                                                                    {
                                                                        num_service = ii;
                                                                    }
                                                                }


                                                            }

                                                            if (num_service != -1)
                                                            {
                                                                if (projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf + projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta * 1440f <= st[pivot].h)
                                                                {

                                                                    if (st[predecesseur].cout > st[pivot].cout + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hd - projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta * 1440f + st[pivot].h) * projet.param_affectation_horaire.cveh[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type] && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf <= projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].hd))
                                                                    {
                                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                        gga_nq[bucket].Remove(predecesseur);
                                                                        st[predecesseur].service = num_service;
                                                                        st_delta[predecesseur][num_service] = projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].delta;

                                                                        st[predecesseur].touche = 2;
                                                                        st[predecesseur].cout = st[pivot].cout + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h) * projet.param_affectation_horaire.cveh[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                        st[predecesseur].h = projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 60f * 24f;
                                                                        st[predecesseur].tatt = st[pivot].tatt;
                                                                        st[predecesseur].tatt1 = st[pivot].tatt1;
                                                                        st[predecesseur].tveh = st[pivot].tveh - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd /*- projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f*/ + st[pivot].h;
                                                                        st[predecesseur].tcor = st[pivot].tcor;
                                                                        st[predecesseur].ncorr = st[pivot].ncorr;
                                                                        st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                                        st[predecesseur].tmap = st[pivot].tmap;
                                                                        st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                        st[predecesseur].pivot = pivot;
                                                                        st[predecesseur].turn_pivot = j;
                                                                        st[predecesseur].pole = st[pivot].pole;
                                                                        st[predecesseur].poleV2 = st[pivot].poleV2;

                                                                        //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                        bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                        gga_nq[bucket].Add(predecesseur);
                                                                        nb_pop_local++;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        //successeurs TC lignes différentes
                                                        else if ((projet.reseaux[projet.reseau_actif].links[predecesseur].ligne != projet.reseaux[projet.reseau_actif].links[pivot].ligne && projet.param_affectation_horaire.cveh[pred_type] > 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0 && projet.reseaux[projet.reseau_actif].links[predecesseur].ligne > 0) && (st[predecesseur].cout > st[pivot].cout))
                                                        {
                                                            int ii, jj, num_service = -1, h3 = -1, delta, duree_periode;
                                                            float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; ii++)
                                                            {
                                                                delta = 0;
                                                                duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Length;

                                                                if (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + temps_correspondance > st[pivot].h || projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                                                                {

                                                                    h1 = -1e38f;
                                                                    h2 = 1e38f;
                                                                    h3 = -1;
                                                                    for (jj = jour - 1; jj >= Math.Max(jour - projet.param_affectation_horaire.nb_jours, 0); jj--)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) + temps_correspondance < st[pivot].h)
                                                                        {
                                                                            h1 = projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                                                            h2 = (-jour + jj);
                                                                            h3 = jj;
                                                                        }

                                                                    }
                                                                    if (h3 != -1)
                                                                    {
                                                                        if (st_delta[predecesseur][ii] > h2 || st[predecesseur].touche == 0)
                                                                        {
                                                                            st_delta[predecesseur][ii] = h2;
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        delta = 1;
                                                                    }


                                                                }
                                                                if ((projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + st_delta[predecesseur][ii] * 1440f + max_correspondance >= st[pivot].h) && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + st_delta[predecesseur][ii] * 1440f + temps_correspondance <= st[pivot].h))

                                                                {
                                                                    if (st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[pivot_type]) + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type] < cout2 && delta < 1)
                                                                    {
                                                                        cout2 = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[pivot_type]) + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                        num_service = ii;
                                                                    }
                                                                }

                                                            }

                                                            if (num_service != -1)
                                                            {
                                                                if (st[predecesseur].cout > st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - st_delta[predecesseur][num_service] * 1440f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[pivot_type]) + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type])
                                                                {
                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Remove(predecesseur);
                                                                    st[predecesseur].service = num_service;
                                                                    st[predecesseur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - st_delta[predecesseur][num_service] * 1440f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + (temps_correspondance * projet.param_affectation_horaire.cboa[pivot_type]) + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                    st[predecesseur].touche = 2;

                                                                    st[predecesseur].h = projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 60f * 24f;
                                                                    if (st[pivot].tatt1 == 0)
                                                                    {
                                                                        st[predecesseur].tatt1 = -projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h;
                                                                    }
                                                                    else
                                                                    {
                                                                        st[predecesseur].tatt1 = st[pivot].tatt1;

                                                                    }
                                                                    st[predecesseur].tatt = st[pivot].tatt - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h;
                                                                    st[predecesseur].tveh = st[pivot].tveh + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd;
                                                                    st[predecesseur].tcor = st[pivot].tcor + temps_correspondance;
                                                                    st[predecesseur].ncorr = st[pivot].ncorr + 1;
                                                                    st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                                    st[predecesseur].tmap = st[pivot].tmap;
                                                                    st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                    st[predecesseur].pivot = pivot;
                                                                    st[predecesseur].turn_pivot = j;
                                                                    st[predecesseur].pole = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i;
                                                                    st[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i + "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i + st[pivot].poleV2;

                                                                    //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Add(predecesseur);
                                                                    nb_pop_local++;
                                                                }
                                                            }

                                                        }
                                                        //predecesseurs TC lignes différentes pivot MAP
                                                        else if ((projet.reseaux[projet.reseau_actif].links[predecesseur].ligne > 0 && projet.reseaux[projet.reseau_actif].links[predecesseur].ligne != projet.reseaux[projet.reseau_actif].links[pivot].ligne && projet.param_affectation_horaire.cveh[pred_type] > 0 && projet.reseaux[projet.reseau_actif].links[pivot].ligne < 0) && (st[predecesseur].cout > st[pivot].cout))
                                                        {
                                                            int ii, jj, num_service = -1, h3 = -1, delta, duree_periode;
                                                            float h1 = 1e38f, h2 = 1e38f, cout2 = 1e38f;
                                                            for (ii = 0; ii < projet.reseaux[projet.reseau_actif].links[predecesseur].services.Count; ii++)
                                                            {
                                                                delta = 0;

                                                                duree_periode = projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Length;

                                                                if (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + temps_correspondance > st[pivot].h || projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jour, 1) == "N")
                                                                {

                                                                    h1 = -1e38f;
                                                                    h2 = 1e38f;
                                                                    h3 = -1;
                                                                    for (jj = jour - 1; jj >= Math.Max(jour - projet.param_affectation_horaire.nb_jours, 0); jj--)
                                                                    {
                                                                        if (projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].regime].Substring(jj, 1) == "O" && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) > h1 && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f) < st[pivot].h)
                                                                        {
                                                                            h1 = projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + (-jour + jj) * 24f * 60f;
                                                                            h2 = (-jour + jj);
                                                                            h3 = jj;
                                                                        }

                                                                    }
                                                                    if (h3 != -1)
                                                                    {
                                                                        if (st_delta[predecesseur][ii] > h2 || st[predecesseur].touche == 0)
                                                                        {
                                                                            st_delta[predecesseur][ii] = h2;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        delta = 1;
                                                                    }


                                                                }
                                                                if ((projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + st_delta[predecesseur][ii] * 1440f + max_correspondance >= st[pivot].h) && (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf + st_delta[predecesseur][ii] * 1440f + temps_correspondance <= st[pivot].h))

                                                                {
                                                                    if (st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type] < cout2 && delta < 1)
                                                                    {
                                                                        num_service = ii;
                                                                        cout2 = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[ii].hf - st_delta[predecesseur][ii] * 60f * 24f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                    }
                                                                }
                                                            }

                                                            if (num_service != -1)
                                                            {
                                                                if (st[predecesseur].cout > st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - st_delta[predecesseur][num_service] * 1440f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type])
                                                                {

                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Remove(predecesseur);
                                                                    st[predecesseur].service = num_service;
                                                                    st[predecesseur].cout = st[pivot].cout + (projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hd) * projet.param_affectation_horaire.cveh[pred_type] + (-projet.reseaux[projet.reseau_actif].links[predecesseur].services[num_service].hf - st_delta[predecesseur][num_service] * 1440f + st[pivot].h) * projet.param_affectation_horaire.cwait[pred_type] + projet.reseaux[projet.reseau_actif].links[predecesseur].toll * projet.param_affectation_horaire.ctoll[pred_type];
                                                                    st[predecesseur].touche = 2;

                                                                    st[predecesseur].h = projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 60f * 24f;
                                                                    if (st[pivot].tatt1 == 0)
                                                                    {
                                                                        st[predecesseur].tatt1 = -projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h;
                                                                    }
                                                                    else
                                                                    {
                                                                        st[predecesseur].tatt1 = st[pivot].tatt1;

                                                                    }
                                                                    st[predecesseur].tatt = st[pivot].tatt - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].delta * 1440f + st[pivot].h;
                                                                    st[predecesseur].tveh = st[pivot].tveh + projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hf - projet.reseaux[projet.reseau_actif].links[predecesseur].services[st[predecesseur].service].hd;
                                                                    st[predecesseur].tcor = st[pivot].tcor;
                                                                    st[predecesseur].ncorr = st[pivot].ncorr;
                                                                    st[predecesseur].tmap = st[pivot].tmap;
                                                                    st[predecesseur].ttoll = st[pivot].ttoll + projet.reseaux[projet.reseau_actif].links[predecesseur].toll;

                                                                    st[predecesseur].l = st[pivot].l + projet.reseaux[projet.reseau_actif].links[predecesseur].longueur;
                                                                    st[predecesseur].pivot = pivot;
                                                                    st[predecesseur].turn_pivot = j;
                                                                    st[predecesseur].pole = st[pivot].pole;
                                                                    st[predecesseur].poleV2 = "|" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i + st[pivot].poleV2;
                                                                    //bucket = (int)Math.Truncate(Math.Min((Math.Pow(st[predecesseur].cout, 2) / projet.param_affectation_horaire.param_dijkstra), projet.param_affectation_horaire.max_nb_buckets));
                                                                    bucket = Convert.ToInt32(Math.Truncate(Math.Min(Math.Pow(st[predecesseur].cout / projet.param_affectation_horaire.param_dijkstra, projet.param_affectation_horaire.pu), projet.param_affectation_horaire.max_nb_buckets - 1)));
                                                                    gga_nq[bucket].Add(predecesseur);
                                                                    nb_pop_local++;
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
                                                        String texto = "pivot:" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].no].i.ToString() + " " +
                                                           projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].nd].i.ToString() + " " + 
                                                            st[pivot].h.ToString() +
                                                            " " + st[pivot].cout.ToString() +
                                                            " " + st[pivot].tmap.ToString()+
                                                            " " + st[pivot].tveh.ToString()  +
                                                            " " + st[pivot].tatt.ToString() + '\n' +
                                                            "pred:" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].no].i.ToString() + " " +
                                                           projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[predecesseur].nd].i.ToString() + 
                                                            " " + st[predecesseur].h.ToString() +
                                                            " " + st[predecesseur].cout.ToString() +
                                                            " " + st[predecesseur].tmap.ToString()+
                                                            " " + st[predecesseur].tveh.ToString() +
                                                            " " + st[predecesseur].tatt.ToString() +'\n'; 
                                                        fich_log.Write(texto);
                                                    }*/

                                                }
                                            }
                                            //st[pivot].touche = 3;
                                            //Console.WriteLine((touches.Count+calcules.Count).ToString());
                                        }
                                    fin_gga2:
                                        //Console.WriteLine(p.ToString());

                                        int arrivee = -1;
                                        double cout_fin = 1e38f;
                                        if (projet.reseaux[projet.reseau_actif].numnoeud.TryGetValue(p, out value) == true)
                                        {
                                            for (j = 0; j < projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[p]].succ.Count; j++)
                                            {
                                                predecesseur = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].numnoeud[p]].succ[j];
                                                if (st[predecesseur].touche != 0 && st[predecesseur].cout < cout_fin)
                                                {
                                                    arrivee = predecesseur;
                                                    cout_fin = st[predecesseur].cout;

                                                }




                                            }
                                        }
                                        else
                                        {
                                            fich_log.WriteLine("OD error" + libod + ":" + ":" + chaine + ": non existing origin node!");
                                        }

                                        if (arrivee != -1)
                                        {
                                            if (projet.reseaux[projet.reseau_actif].links[arrivee].ligne > 0)
                                            {
                                                st[arrivee].boai += od;
                                                svc_tmp[arrivee][st[arrivee].service].boai = od;
                                                svc_tmp[arrivee][st[arrivee].service].boat += od;
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
                                            if (projet.reseaux[projet.reseau_actif].links[pivot].texte != null)
                                            {

                                                lignes_corr = projet.reseaux[projet.reseau_actif].links[pivot].texte.Split(param2, StringSplitOptions.RemoveEmptyEntries);
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
                                            st[pivot].volau += od;
                                            if (st[pivot].pivot != -1 && projet.param_affectation_horaire.sortie_turns == true)
                                            {
                                                Turn virage = new Turn();
                                                virage.arci = pivot;
                                                virage.arcj = st[pivot].pivot;
                                                float value2;
                                                if (transfers.TryGetValue(virage, out value2) == true)
                                                {

                                                    transfers[virage] += od;
                                                }
                                                else
                                                {
                                                    transfers[virage] = od;
                                                }

                                                //projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].arci[st[pivot].turn_pivot].volau += od;
                                            }
                                            if (st[pivot].service >= 0)
                                            {
                                                svc_tmp[pivot][st[pivot].service].volau += od;
                                            }

                                            if (st[pivot].pivot == -1)
                                            {
                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                {
                                                    st[pivot].alij += od;
                                                    svc_tmp[pivot][st[pivot].service].alij = od;
                                                    svc_tmp[pivot][st[pivot].service].alit += od;

                                                }
                                            }
                                            else if (projet.reseaux[projet.reseau_actif].links[pivot].ligne != projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].ligne)
                                            {
                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne > 0)
                                                {
                                                    st[pivot].alij += od;
                                                    svc_tmp[pivot][st[pivot].service].alij = od;
                                                    svc_tmp[pivot][st[pivot].service].alit += od;

                                                }
                                                if (projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].ligne > 0)
                                                {
                                                    st[st[pivot].pivot].boai += od;
                                                    svc_tmp[st[pivot].pivot][st[st[pivot].pivot].service].boai = od;
                                                    svc_tmp[st[pivot].pivot][st[st[pivot].pivot].service].boat += od;
                                                }

                                            }
                                            if (projet.param_affectation_horaire.sortie_chemins == true)
                                            {
                                                texte = libod + ";" + p + ";" + q + ";" + jour.ToString("0") + ";" + horaire.ToString("0.000");
                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].no].i;
                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].nd].i;
                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].no].i + "-" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[pivot].nd].i;

                                                texte += ";" + projet.reseaux[projet.reseau_actif].links[pivot].ligne.ToString("0");
                                                if (st[pivot].service >= 0)
                                                {
                                                    texte += ";" + projet.reseaux[projet.reseau_actif].links[pivot].services[st[pivot].service].numero.ToString("0");
                                                }
                                                else
                                                {
                                                    texte += ";-1";
                                                }
                                                texte += ";" + (-st[pivot].h + horaire).ToString("0.000");
                                                texte += ";" + st[pivot].h.ToString("0.000");
                                                texte += ";" + st[pivot].tveh.ToString("0.000");
                                                texte += ";" + st[pivot].tmap.ToString("0.000");
                                                texte += ";" + st[pivot].tatt.ToString("0.000");
                                                texte += ";" + st[pivot].tcor.ToString("0.000");
                                                texte += ";" + st[pivot].ncorr.ToString("0.000");
                                                texte += ";" + st[pivot].tatt1.ToString("0.000");
                                                texte += ";" + st[pivot].cout.ToString("0.000");
                                                texte += ";" + st[pivot].l.ToString("0.000");
                                                texte += ";" + st[pivot].pole;
                                                texte += ";" + od.ToString("0.00");
                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne != -1)
                                                {
                                                    texte += ";" + svc_tmp[pivot][st[pivot].service].boai.ToString("0.000");
                                                    texte += ";" + svc_tmp[pivot][st[pivot].service].alij.ToString("0.000");
                                                    svc_tmp[pivot][st[pivot].service].boai = 0;
                                                    svc_tmp[pivot][st[pivot].service].alij = 0;

                                                }
                                                else
                                                {
                                                    texte += ";0";
                                                    texte += ";0";
                                                }
                                                texte += ";" + projet.reseaux[projet.reseau_actif].links[pivot].texte;
                                                texte += ";" + projet.reseaux[projet.reseau_actif].links[pivot].type;

                                                texte += ";" + st[pivot].ttoll.ToString("0.000");


                                                lignes_gr.chemins.Add(texte);



                                            }
                                            if (st[pivot].pivot != -1)
                                            {
                                                if (projet.reseaux[projet.reseau_actif].links[pivot].ligne != projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].ligne)
                                                {
                                                    string[] param2 = { "|" }, lignes_corr = null;
                                                    if (projet.reseaux[projet.reseau_actif].links[pivot].texte != null)
                                                    {


                                                        lignes_corr = projet.reseaux[projet.reseau_actif].links[st[pivot].pivot].texte.Split(param2, StringSplitOptions.RemoveEmptyEntries);
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
                                            pivot = st[pivot].pivot;
                                        }
                                        if (arrivee != -1)
                                        {
                                            texte = libod + ";" + p + ";" + q;
                                            texte += ";" + jour.ToString("0.000");
                                            texte += ";" + st[arrivee].h.ToString("0.000");
                                            texte += ";" + horaire.ToString("0.000");
                                            texte += ";" + (horaire - st[arrivee].h).ToString("0.000");
                                            texte += ";" + st[arrivee].tveh.ToString("0.000");
                                            texte += ";" + st[arrivee].tmap.ToString("0.000");
                                            texte += ";" + st[arrivee].tatt.ToString("0.000");
                                            texte += ";" + st[arrivee].tcor.ToString("0.000");
                                            texte += ";" + st[arrivee].ncorr.ToString("0.000");
                                            texte += ";" + st[arrivee].tatt1.ToString("0.000");
                                            texte += ";" + st[arrivee].cout.ToString("0.000");
                                            texte += ";" + st[arrivee].l.ToString("0.000");
                                            texte += ";" + st[arrivee].pole;
                                            texte += ";" + od.ToString("0.00");
                                            //itineraire = "MAP," + itineraire;
                                            texte += ";" + itineraire;
                                            texte += ";" + projet.param_affectation_horaire.nb_pop;
                                            texte += ";" + st[arrivee].ttoll.ToString("0.000");



                                            lignes_gr.od_line = texte;

                                            if (projet.param_affectation_horaire.sortie_noeuds == true)
                                            {
                                                foreach (node n in projet.reseaux[projet.reseau_actif].nodes)
                                                {
                                                    float tmax = 1e38f;
                                                    String type_arc = "";
                                                    int which_tmax = -1;
                                                    link troncon = new link();
                                                    for (int s = 0; s < n.succ.Count; s++)
                                                    {
                                                        troncon = projet.reseaux[projet.reseau_actif].links[n.succ[s]];
                                                        if (troncon.cout <= tmax && troncon.touche != 0 && (troncon.ligne <= 0 || projet.param_affectation_horaire.sortie_temps == 2))
                                                        {
                                                            tmax = troncon.cout;
                                                            which_tmax = n.succ[s];
                                                            type_arc = troncon.type;

                                                        }

                                                    }
                                                    if (which_tmax >= 0 && tmax <= projet.param_affectation_horaire.temps_max)
                                                    {
                                                        if (filtre.Contains(type_arc) || filtre.Count == 0)
                                                        {
                                                            if (projet.param_affectation_horaire.sortie_temps == 3)
                                                            {
                                                                texte = q;
                                                                texte += ";" + n.i;
                                                                texte += ";" + (horaire - st[which_tmax].h).ToString("0.000");
                                                                texte += ";" + st[which_tmax].tatt1.ToString("0.000");
                                                                texte += ";" + od.ToString("0.000");

                                                            }
                                                            else
                                                            {
                                                                texte = libod + ";" + p + ";" + q;
                                                                texte += ";" + jour.ToString("0.000");
                                                                texte += ";" + n.i;
                                                                texte += ";" + horaire.ToString("0.000");
                                                                texte += ";" + st[which_tmax].h.ToString("0.000");
                                                                texte += ";" + (horaire - st[which_tmax].h).ToString("0.000");
                                                                texte += ";" + st[which_tmax].tveh.ToString("0.000");
                                                                texte += ";" + st[which_tmax].tmap.ToString("0.000");
                                                                texte += ";" + st[which_tmax].tatt.ToString("0.000");
                                                                texte += ";" + st[which_tmax].tcor.ToString("0.000");
                                                                texte += ";" + st[which_tmax].ncorr.ToString("0");
                                                                texte += ";" + st[which_tmax].tatt1.ToString("0.000");
                                                                texte += ";" + st[which_tmax].cout.ToString("0.000");
                                                                texte += ";" + st[which_tmax].l.ToString("0.000");
                                                                texte += ";" + st[which_tmax].pole;
                                                                texte += ";" + st[which_tmax].ttoll.ToString("0.000");
                                                                texte += ";" + od.ToString("0.000");
                                                                if (projet.param_affectation_horaire.sortie_stops == true)
                                                                {
                                                                    texte += ";" + st[which_tmax].poleV2;
                                                                }
                                                                else
                                                                {
                                                                    texte += ";";
                                                                }
                                                            }
                                                            lignes_gr.noeuds.Add(texte);
                                                        }
                                                    }
                                                }
                                            }
                                            if (projet.param_affectation_horaire.sortie_temps == 0)
                                            {
                                                network reseau = projet.reseaux[projet.reseau_actif];
                                                double som_detour = 0; double nb_detour = 0; double som_oiseau = 0;
                                                double d_oiseau, d_link;

                                                foreach (link li in reseau.links)
                                                {
                                                    d_oiseau = Math.Pow(Math.Pow((reseau.nodes[reseau.numnoeud[q]].x - reseau.nodes[li.no].x), 2) + Math.Pow((reseau.nodes[reseau.numnoeud[q]].y - reseau.nodes[li.no].y), 2), 0.5);
                                                    d_link = Math.Pow(Math.Pow((reseau.nodes[li.no].x - reseau.nodes[li.nd].x), 2) + Math.Pow((reseau.nodes[li.no].y - reseau.nodes[li.nd].y), 2), 0.5);

                                                    if (d_oiseau > 500 & d_oiseau - d_link <= 500)
                                                    {

                                                        if (reseau.nodes[reseau.numnoeud[q]].x > 0 && reseau.nodes[reseau.numnoeud[q]].y > 0 && reseau.nodes[li.no].x > 0 && reseau.nodes[li.no].y > 0)
                                                        {
                                                            if (li.h > 0)
                                                            {
                                                                som_detour += li.h;
                                                                som_oiseau += Math.Pow(Math.Pow((reseau.nodes[reseau.numnoeud[q]].x - reseau.nodes[li.no].x), 2) + Math.Pow((reseau.nodes[reseau.numnoeud[q]].y - reseau.nodes[li.no].y), 2), 0.5);
                                                                nb_detour++;
                                                            }
                                                        }
                                                    }
                                                }
                                                fich_detour.WriteLine(q + ";" + som_detour.ToString() + ";" + som_oiseau.ToString() + ";" + nb_detour.ToString());

                                            }


                                            if (projet.param_affectation_horaire.sortie_temps > 0 && projet.param_affectation_horaire.sortie_temps < 4)
                                            {
                                                for (i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
                                                {

                                                    arrivee = i;
                                                    if (filtre.Contains(projet.reseaux[projet.reseau_actif].links[arrivee].type) || filtre.Count == 0)
                                                    {

                                                        if (st[arrivee].touche != 0 && (projet.reseaux[projet.reseau_actif].links[arrivee].ligne < 0 || projet.param_affectation_horaire.sortie_temps == 2))
                                                        {
                                                            if (projet.param_affectation_horaire.sortie_noeuds == false && projet.param_affectation_horaire.sortie_temps == 3)
                                                            {
                                                                texte = q;
                                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].no].i;
                                                                texte += "-" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].nd].i;
                                                                texte += ";" + (horaire - st[arrivee].h).ToString("0.000");
                                                                texte += ";" + st[arrivee].tatt1.ToString("0.000");
                                                                texte += ";" + od.ToString("0.000");


                                                            }
                                                            else if (projet.param_affectation_horaire.sortie_temps < 3)
                                                            {

                                                                texte = libod + ";" + q;
                                                                texte += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].no].i;
                                                                texte += "-" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].nd].i;
                                                                texte += ";" + (projet.reseaux[projet.reseau_actif].links[arrivee].ligne).ToString("0");
                                                                texte += ";" + i.ToString("0");
                                                                texte += ";" + jour.ToString("0");


                                                                texte += ";" + st[arrivee].h.ToString("0.000");
                                                                texte += ";" + horaire.ToString("0.000");
                                                                link arc = new link();
                                                                arc = projet.reseaux[projet.reseau_actif].links[arrivee];
                                                                float ti;
                                                                if (arc.ligne < 0)
                                                                {
                                                                    ti = horaire - (arc.h + arc.temps * projet.param_affectation_horaire.coef_tmap[arc.type]);
                                                                }
                                                                else
                                                                {
                                                                    ti = horaire - (arc.h + (arc.services[arc.service].hf - arc.services[arc.service].hd));
                                                                }
                                                                texte += ";" + ti.ToString("0.000");

                                                                texte += ";" + st[arrivee].tveh.ToString("0.000");
                                                                texte += ";" + st[arrivee].tmap.ToString("0.000");
                                                                texte += ";" + st[arrivee].tatt.ToString("0.000");
                                                                texte += ";" + st[arrivee].tcor.ToString("0.000");
                                                                texte += ";" + st[arrivee].ncorr.ToString("0.000");
                                                                texte += ";" + st[arrivee].tatt1.ToString("0.000");
                                                                texte += ";" + st[arrivee].cout.ToString("0.000");
                                                                texte += ";" + st[arrivee].l.ToString("0.000");
                                                                texte += ";" + st[arrivee].pole;
                                                                texte += ";" + od.ToString("0.00");
                                                                //texte += ";" + projet.reseaux[projet.reseau_actif].links[arrivee].texte;
                                                                /*texte += ";" + projet.param_affectation_horaire.texte_cveh;
                                                                texte += ";" + projet.param_affectation_horaire.texte_cwait;
                                                                texte += ";" + projet.param_affectation_horaire.texte_cmap;
                                                                texte += ";" + projet.param_affectation_horaire.texte_cboa;
                                                                texte += ";" + projet.param_affectation_horaire.texte_coef_tmap;
                                                                texte += ";" + projet.param_affectation_horaire.texte_tboa;
                                                                texte += ";" + projet.param_affectation_horaire.nb_jours;*/
                                                                texte += ";" + st[arrivee].pivot.ToString("0");
                                                                texte += ";" + projet.reseaux[projet.reseau_actif].links[arrivee].type;
                                                                texte += ";" + st[arrivee].ttoll.ToString("0.000");
                                                                texte += ";" + (horaire - st[arrivee].h).ToString("0.000");

                                                            }


                                                            //                                itineraire = "MAP," + itineraire;
                                                            if (st[arrivee].cout <= projet.param_affectation_horaire.temps_max)
                                                            {
                                                                if (test_temps_per_min_arrivee(projet, arrivee) == true)
                                                                {
                                                                    if (!(projet.param_affectation_horaire.sortie_temps == 3 && projet.param_affectation_horaire.sortie_noeuds == true))
                                                                    {
                                                                        lignes_gr.temps.Add(texte);
                                                                    }
                                                                }


                                                            }
                                                        }
                                                        else if (st[arrivee].touche == 0 && projet.param_affectation_horaire.sortie_isoles == true)
                                                        {
                                                            texte = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].no].i;
                                                            texte += "-" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[arrivee].nd].i;
                                                            texte += ";" + (projet.reseaux[projet.reseau_actif].links[arrivee].ligne).ToString("0");
                                                            texte += ";" + i.ToString("0");
                                                            lignes_gr.isoles.Add(texte);

                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }


                                    // Collecter les accumulations dans lignes_gr
                                    for (int ii4 = 0; ii4 < nb_links_tot; ii4++)
                                    {
                                        if (st[ii4].volau > 0 || st[ii4].boai > 0 || st[ii4].alij > 0
                                            || (svc_tmp[ii4].Length > 0 && st[ii4].service >= 0
                                                && st[ii4].service < svc_tmp[ii4].Length
                                                && (svc_tmp[ii4][st[ii4].service].volau > 0
                                                    || svc_tmp[ii4][st[ii4].service].boai > 0
                                                    || svc_tmp[ii4][st[ii4].service].alij > 0)))
                                        {
                                            int s4 = st[ii4].service;
                                            lignes_gr.accum.Add((
                                                ii4,
                                                st[ii4].volau,
                                                s4,
                                                s4 >= 0 && s4 < svc_tmp[ii4].Length ? svc_tmp[ii4][s4].boai : 0f,
                                                s4 >= 0 && s4 < svc_tmp[ii4].Length ? svc_tmp[ii4][s4].boat : 0f,
                                                s4 >= 0 && s4 < svc_tmp[ii4].Length ? svc_tmp[ii4][s4].alij : 0f,
                                                s4 >= 0 && s4 < svc_tmp[ii4].Length ? svc_tmp[ii4][s4].alit : 0f
                                            ));
                                        }
                                    }
                                    // Reset st volau/boai/alij pour la prochaine paire
                                    for (int ii5 = 0; ii5 < nb_links_tot; ii5++)
                                    {
                                        st[ii5].volau = 0;
                                        st[ii5].boai = 0;
                                        st[ii5].alij = 0;
                                    }

                                } // foreach paire

                                Interlocked.Increment(ref gr_done);
                            }); // Parallel.ForEach

                        thread_st.Dispose();
                        thread_delta.Dispose();
                        timer_av.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                        Console.SetCursorPosition(cleft2, ctop2);
                        Console.WriteLine("Shortest paths computing...:100%");

                        // 5. Accumulation séquentielle volau/boai/alij
                        foreach (var res2 in resultats)
                        {
                            if (res2 == null) continue;
                            foreach (var (idx2, volau2, svc2, boai2, boat2, alij2, alit2) in res2.accum)
                            {
                                projet.reseaux[projet.reseau_actif].links[idx2].volau += volau2;
                                if (svc2 >= 0 && svc2 < projet.reseaux[projet.reseau_actif].links[idx2].services.Count)
                                {
                                    projet.reseaux[projet.reseau_actif].links[idx2].services[svc2].volau += volau2;
                                    projet.reseaux[projet.reseau_actif].links[idx2].services[svc2].boai += boai2;
                                    projet.reseaux[projet.reseau_actif].links[idx2].services[svc2].boat += boat2;
                                    projet.reseaux[projet.reseau_actif].links[idx2].services[svc2].alij += alij2;
                                    projet.reseaux[projet.reseau_actif].links[idx2].services[svc2].alit += alit2;
                                }
                                projet.reseaux[projet.reseau_actif].links[idx2].boai += boai2;
                                projet.reseaux[projet.reseau_actif].links[idx2].alij += alij2;
                            }
                        }

                        // 6. Écriture fichiers de sortie (ordre d'insertion des groupes)
                        foreach (var res2 in resultats)
                        {
                            if (res2 == null) continue;
                            if (res2.od_line != null) fich_od.WriteLine(res2.od_line);
                            foreach (var l2 in res2.chemins) fich_sortie2.WriteLine(l2);
                            foreach (var l2 in res2.temps) fich_sortie.WriteLine(l2);
                            foreach (var l2 in res2.noeuds) fich_noeuds.WriteLine(l2);
                            if (res2.detour_line != null) fich_detour.WriteLine(res2.detour_line);
                            foreach (var l2 in res2.isoles) fich_isoles.WriteLine(l2);
                        }

                        DateTime t2 = DateTime.Now;
                        fich_log.WriteLine("Computation end time: " + t2.ToString("dddd dd MMMM yyyy HH:mm:ss.fff"));
                        fich_log.WriteLine("Computation duration:" + t2.Subtract(t1).TotalSeconds + " sec");
                        fich_log.Close();

                        fich_sortie.Close();
                        fich_sortie2.Close();
                        fich_od.Close();
                        fich_noeuds.Close();
                        fich_detour.Close();
                        fich_isoles.Close();




                        for (i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
                        {
                            if (projet.reseaux[projet.reseau_actif].links[i].volau > 0 || projet.reseaux[projet.reseau_actif].links[i].boai > 0 || projet.reseaux[projet.reseau_actif].links[i].alij > 0)
                            {
                                string texte_aff = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].no].i;
                                texte_aff += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].nd].i + ";" + projet.reseaux[projet.reseau_actif].links[i].ligne.ToString("0");
                                texte_aff += ";" + projet.reseaux[projet.reseau_actif].links[i].volau.ToString("0.00");
                                texte_aff += ";" + projet.reseaux[projet.reseau_actif].links[i].boai.ToString("0.00");
                                texte_aff += ";" + projet.reseaux[projet.reseau_actif].links[i].alij.ToString("0.00");
                                texte_aff += ";" + projet.reseaux[projet.reseau_actif].links[i].texte;
                                texte_aff += ";" + projet.reseaux[projet.reseau_actif].links[i].type;
                                texte_aff += ";" + projet.reseaux[projet.reseau_actif].links[i].toll.ToString("0.000");

                                fich_result.WriteLine(texte_aff);
                            }

                        }


                        fich_result.Close();
                        //projet.reseaux.Remove(projet.reseaux[projet.reseau_actif]);
                        if (projet.param_affectation_horaire.sortie_services == true)
                        {
                            System.IO.StreamWriter fich_services = new System.IO.StreamWriter(projet.param_affectation_horaire.nom_sortie + "_services.txt", false, System.Text.Encoding.UTF8);
                            string texte_svc = "";
                            fich_services.WriteLine("i;j;ligne;service;hd;hf;regime;volau;boia;alij;texte;type");
                            for (i = 0; i < projet.reseaux[projet.reseau_actif].links.Count; i++)
                            {
                                if (projet.reseaux[projet.reseau_actif].links[i].services.Count > 0)
                                {
                                    for (j = 0; j < projet.reseaux[projet.reseau_actif].links[i].services.Count; j++)
                                    {
                                        if (projet.reseaux[projet.reseau_actif].links[i].services[j].volau > 0)
                                        {
                                            texte_svc = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].no].i;
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[i].nd].i + ";" + projet.reseaux[projet.reseau_actif].links[i].ligne.ToString("0");
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].links[i].services[j].numero;
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].links[i].services[j].hd;
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].links[i].services[j].hf;
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].nom_calendrier[projet.reseaux[projet.reseau_actif].links[i].services[j].regime];
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].links[i].services[j].volau.ToString("0.00");
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].links[i].services[j].boat.ToString("0.00");
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].links[i].services[j].alit.ToString("0.00");
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].links[i].texte;
                                            texte_svc += ";" + projet.reseaux[projet.reseau_actif].links[i].type;
                                            fich_services.WriteLine(texte_svc);

                                        }
                                    }
                                }


                            }
                            fich_services.Close();

                        }
                        if (projet.param_affectation_horaire.sortie_turns == true)
                        {
                            System.IO.StreamWriter fich_turns = new System.IO.StreamWriter(projet.param_affectation_horaire.nom_sortie + "_transferts.txt", false, System.Text.Encoding.UTF8);
                            string texte_turns = "";
                            //int k=0;
                            fich_turns.WriteLine("j;i;lignei;k;lignek;textei;textek;volau");

                            foreach (Turn virage in transfers.Keys)
                            {

                                if (transfers[virage] > 0)
                                {
                                    texte_turns = projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[virage.arci].nd].i;

                                    texte_turns += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[virage.arci].no].i;
                                    texte_turns += ";" + projet.reseaux[projet.reseau_actif].links[virage.arci].ligne;
                                    texte_turns += ";" + projet.reseaux[projet.reseau_actif].nodes[projet.reseaux[projet.reseau_actif].links[virage.arcj].nd].i;
                                    texte_turns += ";" + projet.reseaux[projet.reseau_actif].links[virage.arcj].ligne;
                                    texte_turns += ";" + projet.reseaux[projet.reseau_actif].links[virage.arci].texte;
                                    texte_turns += ";" + projet.reseaux[projet.reseau_actif].links[virage.arcj].texte;
                                    texte_turns += ";" + transfers[virage];
                                    fich_turns.WriteLine(texte_turns);
                                }










                            }
                            fich_turns.Close();

                        }


                    }


                    //algorithme de Dijkstra

                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra
                    //algorithme de Dijkstra


                    //algorithme de Dijkstra


                } // if fichiers existent

            } // if parametres existe

        } // affectation_tc

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
                {
                    return true;
                }
                else
                {
                    return false;
                }
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
            //public float volau=0;

        }

        public class node
        {
            public float x, y, tempst = 1e38f, tmap = 0, tatt, temps, cout, ncor, ttoll;
            public string i;
            public bool ci = false, is_valid = true, is_visible = false, is_intersection = false;
            public List<int> pred = new List<int>();
            public List<int> succ = new List<int>();
            //public List<float> ui = new List<float>();
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
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public override int GetHashCode()
            {
                return (i.GetHashCode() ^ j.GetHashCode() ^ line.GetHashCode());
            }
        }
        public class link
        {
            public float longueur, temps, cout, v0, vsat, tatt, tcor, tveh, tmap, tatt1, a, b, n, volau, lanes, h, l, alij, boai, ncorr, toll = 0, ttoll;
            public int no, nd, service, vdf, touche, pivot, ligne, turn_pivot = -1;//,nb_voies;
            public bool is_queue, is_valid = true;
            //public List<turn> arci = new List<turn>();
            //public List<turn> arcj = new List<turn>();
            //public List<float> ul = new List<float>();
            public List<Service> services = new List<Service>();
            public string texte, modes, pole, type = "0", poleV2;
            /*public float fd(float volau, float len, float precha, float cap, float v0, float a, float b, float n)
            {
                float vc, t0, delay;
                t0 = len / v0;
                vc = (volau + precha) / cap;
                if (vc < 1)
                {
                    delay = t0 * (a - b * vc) / (a - vc);
                }
                else
                {
                    delay = t0 * ((a * (1f - b)) / (n * (a - 1f) * (a - 1f))) * (float)Math.Pow(vc, n) + (n * (a - b) * (a - 1f) - a * (1f - b)) / (n * (a - 1f) * (a - 1f));
                }
                return delay;
            }*/
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

        public static bool test_temps_per_min_depart(etude projet, int arrivee)
        {

            bool reponse = true;
            link arc = new link();
            arc = projet.reseaux[projet.reseau_actif].links[arrivee];
            int ni;
            ni = arc.no;
            foreach (int k in projet.reseaux[projet.reseau_actif].nodes[ni].succ)
            {
                int nj = projet.reseaux[projet.reseau_actif].links[k].nd;
                int ligne = projet.reseaux[projet.reseau_actif].links[k].ligne;
                if ((nj == arc.nd) && !(arc.ligne == ligne) && projet.reseaux[projet.reseau_actif].links[k].touche != 0)
                {
                    if (arc.cout > projet.reseaux[projet.reseau_actif].links[k].cout)
                    {
                        reponse = false;
                    }
                }
            }
            return reponse;
        }
        public static bool test_temps_per_min_arrivee(etude projet, int arrivee)
        {

            bool reponse = true;
            link arc = new link();
            arc = projet.reseaux[projet.reseau_actif].links[arrivee];
            int nj;
            nj = arc.nd;
            foreach (int k in projet.reseaux[projet.reseau_actif].nodes[nj].pred)
            {
                int ni = projet.reseaux[projet.reseau_actif].links[k].no;
                int ligne = projet.reseaux[projet.reseau_actif].links[k].ligne;
                if ((ni == arc.no) && !(arc.ligne == ligne) && projet.reseaux[projet.reseau_actif].links[k].touche != 0)
                {
                    if (arc.cout > projet.reseaux[projet.reseau_actif].links[k].cout)
                    {
                        reponse = false;
                    }
                }
            }
            return reponse;
        }
    }
}