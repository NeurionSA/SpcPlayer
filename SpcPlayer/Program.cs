using SpcPlayer.SPC;
using SpcPlayer.Forms;
using System.Text;

namespace SpcPlayer
{
    internal static class Program
    {

        //private static void testSpcCoreBatch(string path)
        //{
        //    SPCCore core = new SPCCore();
        //    string[] fileList = Directory.GetFiles(path, "*.spc");
        //    string lastSuccess = "clay-01 - Nintendo Logo.spc";
        //    bool skippedToLast = true;

        //    Console.WriteLine("Performing batch testing of 3 minutes of SPC Core emulation with files in:");
        //    Console.WriteLine(path);
        //    Console.WriteLine();

        //    foreach (string file in fileList)
        //    {
        //        // skip past all the files we tested successfully
        //        if (!skippedToLast)
        //        {
        //            if (!Path.GetFileName(file).Equals(lastSuccess)) continue;
        //            skippedToLast = true;
        //            continue;
        //        }

        //        Console.Write("Testing {0,-50}", Path.GetFileName(file));
        //        core.LoadSPC(new SPCFile(file));

        //        while (core.Step()) { }
        //        Console.WriteLine("Success!");
        //    }
        //}

        //private static void testSpcCore()
        //{

        //    SPCFile spcFile = new SPCFile("C:\\Data Alpha\\Video Game Music Rips\\SNES\\Ogre Battle\\ogre-06 - Revolt.spc");
        //    SPCCore core = new SPCCore();
        //    core.LoadSPC(spcFile);
        //    while (core.Step())
        //    {
        //    }
        //    Console.WriteLine("Successfully emulated 3 minutes of playback.");
        //}

        //private static void testSpcPlayback()
        //{
        //    SPCPlayer player = new SPCPlayer();

        //    player.LoadSPC("C:\\Data Alpha\\Video Game Music Rips\\SNES\\Ogre Battle\\ogre-06 - Revolt.spc");

        //    Console.WriteLine("Testing playback. Press any key to stop.");

        //    player.Play();
        //    Console.ReadKey(true);
        //    player.Stop();
        //}

        private static void testSpcPlayerForm()
        {
            FormPlayer player = new FormPlayer();

            player.ShowDialog();
        }

        private static void waitOnKey()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.WriteLine();
        }

        private static void envelopeGenerator()
        {
            FormEnvelopeGenerator eg = new FormEnvelopeGenerator();

            eg.ShowDialog();
        }

        private static void testSongAnalysisForm()
        {
            FormSongAnalysis sa = new FormSongAnalysis();

            sa.ShowDialog();
        }
    

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            //Application.Run(new Form1());

            //testSpcLoad();
            //testSpcCore();
            //testSpcCoreBatch("C:\\Data Alpha\\Video Game Music Rips\\SNES\\Dragon Quest 1-2");
            //testSpcPlayback();
            testSpcPlayerForm();
            //testSongAnalysisForm();

            //envelopeGenerator();
            //waitOnKey();

            Console.WriteLine("Closing, please wait...");
        }
    }
}