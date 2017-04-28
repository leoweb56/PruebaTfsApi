﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
 
namespace BasicSccExample
{
    class Example
    {
        static void Main(string[] args)
        {
            string teamCollection = "https://pruebas88.visualstudio.com/DefaultCollection";
            string teamProject = "$/PruebasTfsVersionControl/sub";

            NetworkCredential netCred = new NetworkCredential(
                "lfdantoni88@hotmail.com",
                "Fate486592");
            BasicAuthCredential basicCred = new BasicAuthCredential(netCred);
            TfsClientCredentials tfsCred = new TfsClientCredentials(basicCred);
            tfsCred.AllowInteractive = false;

            // Get a reference to our Team Foundation Server.
            TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(teamCollection), tfsCred);
            tpc.Authenticate();
            // Get a reference to Version Control.
            VersionControlServer versionControl = tpc.GetService<VersionControlServer>();
 
            // Listen for the Source Control events.
            versionControl.NonFatalError += Example.OnNonFatalError;
            versionControl.Getting += Example.OnGetting;
            versionControl.BeforeCheckinPendingChange += Example.OnBeforeCheckinPendingChange;
            versionControl.NewPendingChange += Example.OnNewPendingChange;
 
            // Create a workspace.
            Workspace workspace = versionControl.CreateWorkspace("BasicSccExample2", versionControl.AuthorizedUser);
 
            String topDir = null;
 
            try
            {
                String localDir = @"c:\temp\BasicSccExample";
                Console.WriteLine("\r\n— Create a mapping: {0} -> {1}", teamProject, localDir);
                workspace.Map(teamProject, localDir);
 
                Console.WriteLine("\r\n— Get the files from the repository.\r\n");
                workspace.Get();
 
                Console.WriteLine("\r\n— Create a file.");
                topDir = workspace.Folders[0].LocalItem; //Path.Combine(workspace.Folders[0].LocalItem, "sub");
                String fileName = Path.Combine(topDir, "basic.cs");
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.WriteLine("revision 16 of basic.cs");
                }
 
                Console.WriteLine("\r\n— Now add everything.\r\n");
                workspace.PendAdd(topDir, true);
 
                Console.WriteLine("\r\n— Show our pending changes.\r\n");
                PendingChange[] pendingChanges = workspace.GetPendingChanges();
                Console.WriteLine("  Your current pending changes:");
                foreach (PendingChange pendingChange in pendingChanges)
                {
                    Console.WriteLine("    path: " + pendingChange.LocalItem +
                                      ", change: " + PendingChange.GetLocalizedStringForChangeType(pendingChange.ChangeType));
                }
 

                Console.WriteLine("\r\n— Checkin the items we added.\r\n");
                int changesetNumber = 0;
                try
                {
                     changesetNumber = workspace.CheckIn(pendingChanges, "Sample changes");
                }
                catch (Exception ee )
                {
                    if (ee.Message.Contains("conflict"))
                    {
                        Conflict[] conflicts = workspace.QueryConflicts(new string[] { teamProject }, true);

                        foreach (Conflict conflict in conflicts)
                        {

                                conflict.Resolution = Resolution.AcceptYours;
                                workspace.ResolveConflict(conflict);
                            

                            if (conflict.IsResolved)
                            {
                                workspace.PendEdit(conflict.TargetLocalItem);
                            }
                        }
                    }
                    else
                    {
                        throw ee;
                    }

                    
                }
                Console.WriteLine("  Checked in changeset " + changesetNumber);
 
                Console.WriteLine("\r\n— Checkout and modify the file.\r\n");
                workspace.PendEdit(fileName);
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.WriteLine("revision 27 of basic.cs");
                }
 
                Console.WriteLine("\r\n— Get the pending change and check in the new revision.\r\n");
                pendingChanges = workspace.GetPendingChanges();
                changesetNumber = workspace.CheckIn(pendingChanges, "Modified basic.cs");
                Console.WriteLine("  Checked in changeset " + changesetNumber);
            }
            finally
            {
                if (topDir != null)
                {
                    //Console.WriteLine("\r\n— Delete all of the items under the test project.\r\n");
                    //workspace.PendDelete(topDir, RecursionType.Full);
                    //PendingChange[] pendingChanges = workspace.GetPendingChanges();
                    //if (pendingChanges.Length > 0)
                    //{
                    //    workspace.CheckIn(pendingChanges, "Clean up!");
                    //}

                    //Console.WriteLine("\r\n— Delete the workspace.");
                    workspace.Delete();
                }
            }
        }
 
        internal static void OnNonFatalError(Object sender, ExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                Console.Error.WriteLine("  Non-fatal exception: " + e.Exception.Message);
            }
            else
            {
                Console.Error.WriteLine("  Non-fatal failure: " + e.Failure.Message);
            }
        }
 
        internal static void OnGetting(Object sender, GettingEventArgs e)
        {
            Console.WriteLine("  Getting: " + e.TargetLocalItem + ", status: " + e.Status);
        }
 
        internal static void OnBeforeCheckinPendingChange(Object sender, ProcessingChangeEventArgs e)
        {
            Console.WriteLine("  Checking in " + e.PendingChange.LocalItem);
        }
 
        internal static void OnNewPendingChange(Object sender, PendingChangeEventArgs e)
        {
            Console.WriteLine("  Pending " + PendingChange.GetLocalizedStringForChangeType(e.PendingChange.ChangeType) +
                              " on " + e.PendingChange.LocalItem);
        }
    }
}