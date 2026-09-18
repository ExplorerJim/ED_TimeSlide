using Newtonsoft.Json;
using OpenTK.Input;
using ScottPlot.Colormaps;
using ScottPlot.Interactivity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Lifetime;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public class OrbitRegistry
    {
        #region Thread-Safe Lock Identity Arrays
        private readonly object registryLock = new object();
        #endregion

        #region Core Data Dictionaries
        private Dictionary<string, MasterOrbitAnchor> masterRegistry = new Dictionary<string, MasterOrbitAnchor>();
        private Dictionary<string, StagingOrbitBlock> stagingRegistry = new Dictionary<string, StagingOrbitBlock>();
        #endregion

        #region Read-Only Path Constants
        private readonly string masterRegistryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.MasterOrbitRegistryFileName);
        private readonly string stagingRegistryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.StagingOrbitRegistryFileName);
        #endregion

        public OrbitRegistry()
        {
            // The dictionary structures are initialized as blank objects upon boot instantiation
        }

        #region Public Database Initializers
        public void LoadRegistriesFromDisk()
        {
            lock (registryLock)
            {
                #region Load Master Record Table
                if (File.Exists(masterRegistryPath))
                {
                    try
                    {
                        string json = File.ReadAllText(masterRegistryPath);
                        masterRegistry = JsonConvert.DeserializeObject<Dictionary<string, MasterOrbitAnchor>>(json)
                                         ?? new Dictionary<string, MasterOrbitAnchor>();
                    }
                    catch { masterRegistry = new Dictionary<string, MasterOrbitAnchor>(); }
                }
                #endregion

                #region Load Staging Record Table
                if (File.Exists(stagingRegistryPath))
                {
                    try
                    {
                        string json = File.ReadAllText(stagingRegistryPath);
                        stagingRegistry = JsonConvert.DeserializeObject<Dictionary<string, StagingOrbitBlock>>(json)
                                          ?? new Dictionary<string, StagingOrbitBlock>();
                    }
                    catch { stagingRegistry = new Dictionary<string, StagingOrbitBlock>(); }
                }
                #endregion
            }
        }

        public void SaveRegistriesToDisk()
        {
            lock (registryLock)
            {
                try
                {
                    #region Stream Master Records to File
                    using (StreamWriter sw = new StreamWriter(masterRegistryPath, false))
                    using (JsonTextWriter jw = new JsonTextWriter(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.None };
                        serializer.Serialize(jw, masterRegistry);
                    }
                    #endregion

                    #region Stream Staging Records to File
                    using (StreamWriter sw = new StreamWriter(stagingRegistryPath, false))
                    using (JsonTextWriter jw = new JsonTextWriter(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.None };
                        serializer.Serialize(jw, stagingRegistry);
                    }
                    #endregion
                }
                catch { }
            }
        }
        #endregion

        #region Public Thread-Safe Access Gateways
        public bool TryGetMasterAnchor(string basePlanetKey, out MasterOrbitAnchor anchor)
        {
            lock (registryLock)
            {
                return masterRegistry.TryGetValue(basePlanetKey, out anchor);
            }
        }

        public void GetMasterKeys(out List<string> keys)
        {
            lock (registryLock)
            {
                keys = new List<string>(masterRegistry.Keys);
            }
        }

        public bool GetAllSystemAddressBodyID(out long[] systemAddresses, out long[] bodyIDs)
        {
            systemAddresses = null;
            bodyIDs = null;
            lock (registryLock)
            {
                try
                {
                    List<string> masterkeys = new List<string>(masterRegistry.Keys);

                    systemAddresses = new long[masterkeys.Count];
                    bodyIDs = new long[masterkeys.Count];
                    string[] temp;

                    for (int k = 0; k < masterkeys.Count; k++)
                    {
                        temp = masterkeys[k].Split('_');
                        if (temp.Length == 2)
                        {
                            systemAddresses[k] = Convert.ToInt64(temp[0]);
                            bodyIDs[k] = Convert.ToInt64(temp[1]);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        public bool SplitKeyintoIDs(string key, out long systemID, out long bodyID)
        {
            systemID = -1;
            bodyID = -1;

            string[] temp = key.Split('_');
            if (temp.Length == 2)
            {
                systemID = Convert.ToInt64(temp[0]);
                bodyID = Convert.ToInt64(temp[1]);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ContainsMasterKey(string basePlanetKey)
        {
            lock (registryLock)
            {
                return masterRegistry.ContainsKey(basePlanetKey);
            }
        }

        public void CommitMasterAnchor(string basePlanetKey, MasterOrbitAnchor anchor)
        {
            lock (registryLock)
            {
                masterRegistry[basePlanetKey] = anchor;
            }
        }

        public bool TryGetStagingBlock(string stagingLookupKey, out StagingOrbitBlock stagingBlock)
        {
            lock (registryLock)
            {
                return stagingRegistry.TryGetValue(stagingLookupKey, out stagingBlock);
            }
        }

        public bool TryGetSystemBodyName(string masterLookupKey, out string systemBodyName)
        {
            lock (registryLock)
            {
                MasterOrbitAnchor masterBlock = new MasterOrbitAnchor();
                if (masterRegistry.TryGetValue(masterLookupKey, out masterBlock))
                {
                    systemBodyName = masterBlock.SystemName + " - " + masterBlock.BodyName;
                    return true;
                }
                else
                {
                    systemBodyName = "";
                    return false;
                }
            }
        }

        public void CommitStagingBlock(string stagingLookupKey, StagingOrbitBlock stagingBlock)
        {
            lock (registryLock)
            {
                stagingRegistry[stagingLookupKey] = stagingBlock;
            }
        }

        public void RemoveStagingBlock(string stagingLookupKey)
        {
            lock (registryLock)
            {
                stagingRegistry.Remove(stagingLookupKey);
            }
        }

        public int GetMasterCount()
        {
            lock (registryLock)
            {
                return masterRegistry.Count;
            }
        }

        public int GetStagingCount()
        {
            lock (registryLock)
            {
                return stagingRegistry.Count;
            }
        }

        /// <summary>
        /// Returns a thread-safe atomic snapshot array of all keys currently registered 
        /// inside the Master Orbit Registry.
        /// </summary>
        public string[] GetMasterKeysSnapshot()
        {
            lock (registryLock)
            {
                if (masterRegistry == null || masterRegistry.Count == 0)
                {
                    return new string[0];
                }

                // Copy keys into a flat array structure to safely exit the lock scope
                string[] keySnapshotArray = new string[masterRegistry.Count];
                masterRegistry.Keys.CopyTo(keySnapshotArray, 0);
                return keySnapshotArray;
            }
        }
        #endregion
    }
}
