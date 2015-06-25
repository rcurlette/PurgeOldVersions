using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using Tridion.ContentManager.CoreService.Client;

namespace PurgeOldVersions
{
    /// <summary>
    /// Purge old versions of Tridion items from the Tridion Database.
    /// Runs at the command-line and if the 'runInInteractiveMode' config variable is set to true 
    /// then it writes output to a Console Window.
    /// 
    /// Created by Robert Curlette, 25/06/2015
    /// </summary>
    class Program
    {
        static string endpointName = "basicHttp_2013";
        //string endpointName = "netTcp_2013";
        static string userName = ConfigurationManager.AppSettings["user"];
        static string password = ConfigurationManager.AppSettings["pw"];
        static bool writeOutput = Boolean.Parse(ConfigurationManager.AppSettings["runInInteractiveMode"]);

        protected static void Main()
        {
            string publications = ConfigurationManager.AppSettings["publications"];
            string contentFolders = ConfigurationManager.AppSettings["contentFolders"];
            string structureGroups = ConfigurationManager.AppSettings["structureGroups"];
            UInt16 versionsToKeep = 2;

            string[] pubs = publications.Split(',');
            foreach(string pubUri in pubs)
            {               
                PurgeFolderItems(contentFolders, versionsToKeep, pubUri.Trim());

                PurgeStructureGroupItems(structureGroups, versionsToKeep, pubUri.Trim());
            }

            if(writeOutput)
            {
                Console.WriteLine("*********** DONE AT " + System.DateTime.Now.ToString() + " ******************");
                Console.ReadLine();
            }
            
        }

        private static string GetLocalUri(string pubUri, string localUri)
        {
            // tcm:0-5-1
            string pubId = pubUri.Split('-')[1];
            string[] itemUriParts = localUri.Split('-');
            string itemUriNoTcm = itemUriParts[0].Replace("tcm:", "");
            itemUriNoTcm = pubId;
            string newUri = "tcm:" + itemUriNoTcm + "-" + itemUriParts[1] + "-" + itemUriParts[2];
            return newUri;
        }
     

        private static void PurgeStructureGroups(CoreServiceClient client, string structureGroups, PurgeOldVersionsInstructionData purgeIntructions, UInt16 versionsToKeep, string pubUri)
        {
            int totalPageVersionsRemoved = 0;

            string[] structureGroupUris = structureGroups.Split(',');

            List<LinkToIdentifiableObjectData> itemsToPurge = new List<LinkToIdentifiableObjectData>();

            for (int i = 0; i < structureGroupUris.Length; i++)
            {
                StructureGroupData structureGroup = null;

                try
                {
                    string localUri = GetLocalUri(pubUri.Trim(), structureGroupUris[i]);

                    // Add sub and subsub-folders to list to prevent timeouts...
                    structureGroup = (StructureGroupData)client.Read(localUri, null);
                } 
                catch(Exception ex)
                {
                    continue;
                }

                LinkToIdentifiableObjectData subfolderLink = new LinkToIdentifiableObjectData();
                subfolderLink.IdRef = structureGroup.Id;
                itemsToPurge.Add(subfolderLink);

                var itemTypes = new List<ItemType>();
                itemTypes.Add(ItemType.StructureGroup);

                var filter = new OrganizationalItemItemsFilterData();
                filter.Recursive = true;
                filter.ItemTypes = itemTypes.ToArray();
                purgeIntructions.VersionsToKeep = versionsToKeep;
                purgeIntructions.Recursive = false;

                IdentifiableObjectData[] subSGs = client.GetList(structureGroup.Id, filter);

                foreach (var subSubSG in subSGs)
                {
                    LinkToIdentifiableObjectData structureGroupLink = new LinkToIdentifiableObjectData();
                    structureGroupLink.IdRef = subSubSG.Id;
                    itemsToPurge.Add(structureGroupLink);
                    purgeIntructions.Containers = itemsToPurge.ToArray();

                    try
                    {
                        int versionsCleaned = client.PurgeOldVersions(purgeIntructions);
                        totalPageVersionsRemoved = totalPageVersionsRemoved + versionsCleaned;
                        WriteOutput("Removed " + versionsCleaned.ToString() + " Page Versions");
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    itemsToPurge.Clear();
                }
            }
            WriteOutput("Total Page versions removed is " + totalPageVersionsRemoved.ToString());
        }

        private static void PurgeFolders(CoreServiceClient client, string folders, PurgeOldVersionsInstructionData purgeIntructions, UInt16 versionsToKeep, string pubUri)
        {
            int totalCompVersionsRemoved = 0;

            string[] folderUris = folders.Split(',');

            List<LinkToIdentifiableObjectData> itemsToPurge = new List<LinkToIdentifiableObjectData>();

            for (int i = 0; i < folderUris.Length; i++)
            {
                FolderData subFolder = null;

                // Add sub and subsub-folders to list to prevent timeouts...
                try
                {
                    string localUri = GetLocalUri(pubUri.Trim(), folderUris[i]);
                    subFolder = (FolderData)client.Read(localUri, null);
                } 
                catch(Exception ex)
                {
                    continue;
                }

                LinkToIdentifiableObjectData subfolderLink = new LinkToIdentifiableObjectData();
                subfolderLink.IdRef = subFolder.Id;
                itemsToPurge.Add(subfolderLink);

                var itemTypes = new List<ItemType>();
                //itemTypes.Add(ItemType.Component);
                itemTypes.Add(ItemType.Folder);
                var filter = new OrganizationalItemItemsFilterData();
                filter.Recursive = true;
                filter.ItemTypes = itemTypes.ToArray();

                purgeIntructions.VersionsToKeep = versionsToKeep;
                purgeIntructions.Recursive = false;

                IdentifiableObjectData[] allSubFolders = client.GetList(subFolder.Id, filter);

                foreach (var aFolder in allSubFolders)
                {
                    LinkToIdentifiableObjectData folderLink = new LinkToIdentifiableObjectData();
                    folderLink.IdRef = aFolder.Id;
                    itemsToPurge.Add(folderLink);
                    purgeIntructions.Containers = itemsToPurge.ToArray();

                    try
                    {
                        int versionsCleaned = client.PurgeOldVersions(purgeIntructions);
                        totalCompVersionsRemoved = totalCompVersionsRemoved + versionsCleaned;
                        WriteOutput("Folder " + folderLink.IdRef + " removed " + versionsCleaned.ToString());
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    itemsToPurge.Clear();
                }
            }
            WriteOutput("Total Component versions removed is " + totalCompVersionsRemoved.ToString());
        }

        protected static void PurgeFolderItems(string startFolder, ushort versionsToKeep, string pubUri)
        {
            using (CoreServiceClient client = new CoreServiceClient(endpointName))
            {
                var credentials = CredentialCache.DefaultNetworkCredentials;
                if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                {
                    credentials = new NetworkCredential(userName, password);
                }
                client.ChannelFactory.Credentials.Windows.ClientCredential = credentials;

                if (versionsToKeep < 0)
                    return;

                PurgeOldVersionsInstructionData purgeIntructions = new PurgeOldVersionsInstructionData();

                string folders = startFolder;
                if (folders != "")
                {
                    PurgeFolders(client, folders, purgeIntructions, versionsToKeep, pubUri);
                }
            }
        }

        protected static void PurgeStructureGroupItems(string startSgUri, ushort versionsToKeep, string pubUri)
        {
            using (CoreServiceClient client = new CoreServiceClient(endpointName))
            {
                var credentials = CredentialCache.DefaultNetworkCredentials;
                if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                {
                    credentials = new NetworkCredential(userName, password);
                }
                client.ChannelFactory.Credentials.Windows.ClientCredential = credentials;

                if (versionsToKeep < 0)
                    return;

                PurgeOldVersionsInstructionData purgeIntructions = new PurgeOldVersionsInstructionData();

                string structureGroups = startSgUri;
                if (structureGroups != "")
                {
                    PurgeStructureGroups(client, structureGroups, purgeIntructions, versionsToKeep, pubUri);
                }
            }
        }

        private static void WriteOutput(string text)
        {
            if(writeOutput)
            {
                Console.WriteLine(text);
            }
        }
       
    }
}

