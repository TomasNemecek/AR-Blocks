//-----------------------------------------------------------------------
// <copyright file="LocalPlayerController.cs" company="Google">
//
// Copyright 2018 Google LLC. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.Examples.CloudAnchors
{
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// Local player controller. Handles the spawning of the networked Game Objects.
    /// </summary>

    public class LocalPlayerController : NetworkBehaviour

    {
        /// <summary>
        /// The Star model that will represent networked objects in the scene.
        /// </summary>
        public GameObject StarPrefab;

        /// <summary>
        /// The Anchor model that will represent the anchor in the scene.
        /// </summary>
        public GameObject AnchorPrefab;
        
        public GameObject BlockPrefab;

        /// <summary>
        /// The Unity OnStartLocalPlayer() method.
        /// </summary>
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // A Name is provided to the Game Object so it can be found by other Scripts, since this
            // is instantiated as a prefab in the scene.
            gameObject.name = "LocalPlayer";
        }

        /// <summary>
        /// Will spawn the origin anchor and host the Cloud Anchor. Must be called by the host.
        /// </summary>
        /// <param name="position">Position of the object to be instantiated.</param>
        /// <param name="rotation">Rotation of the object to be instantiated.</param>
        /// <param name="anchor">The ARCore Anchor to be hosted.</param>
        public void SpawnAnchor(Vector3 position, Quaternion rotation, Component anchor)
        {
            // Instantiate Anchor model at the hit pose.
            var anchorObject = Instantiate(AnchorPrefab, position, rotation);

            // Anchor must be hosted in the device.
            anchorObject.GetComponent<AnchorController>().HostLastPlacedAnchor(anchor);

            // Host can spawn directly without using a Command because the server is running in this
            // instance.
            NetworkServer.Spawn(anchorObject);
        }

        /// <summary>
        /// A command run on the server that will spawn the Star prefab in all clients.
        /// </summary>
        /// <param name="position">PositiCmdSpawnStaron of the object to be instantiated.</param>
        /// <param name="rotation">Rotation of the object to be instantiated.</param>

        [Command]
        public void CmdSpawnStar(Vector3 position, Quaternion rotation)
        {
            // Instantiate Star model at the hit pose.
            var starObject = Instantiate(StarPrefab, position, rotation);

            // Spawn the object in all clients.
            NetworkServer.Spawn(starObject);

        }
        
        [Command]
        public void CmdSpawnBlock(Vector3 position, Quaternion rotation)
        {
            // Instantiate Star model at the hit pose.
            var blockObject = Instantiate(BlockPrefab, position, rotation);
            // Spawn the object in all clients.
            NetworkServer.Spawn(blockObject);

        }

        public void OnDestroyBlockForClient(GameObject gameobject)
        {
            RpcDestroy(gameobject.transform.position);
        } 
        
        [ClientRpc]
        void RpcDestroy(Vector3 position)
        {
            GameObject blockAtVector = FindAt(position);    
            Destroy(blockAtVector);
        }
        
        GameObject FindAt(Vector3 position) {
            // get all colliders that intersect pos:
            Collider[] cols = Physics.OverlapSphere(position, 0.1f);
            // find the nearest one:
            float dist = Mathf.Infinity;
            GameObject nearest = null;
            int i = 0;
            while (i < cols.Length)
            {
                // find the distance to pos:
                var d = Vector3.Distance(position, cols[i].transform.position);
                if (d < dist){ // if closer...
                    dist = d; // save its distance... 
                    nearest = cols[i].gameObject; // and its gameObject
                }

                i++;
            }
               
            return nearest;
        }
        
        public void OnDestroyBlockForHost(GameObject gameobject)
        {
            CmdDestroyBlock(gameobject.transform.position);
            Destroy(gameobject);
        } 
        
        
        [Command]
        public void CmdDestroyBlock(Vector3 position)
        {
            GameObject blockAtVector = FindAt(position);          
            Destroy(blockAtVector);
        }       
    }
}
