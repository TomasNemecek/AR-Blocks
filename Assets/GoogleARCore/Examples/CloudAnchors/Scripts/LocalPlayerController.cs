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
        public GameObject BlocksPrefab;
        public GameObject BlocksVertPrefab;
        public GameObject BlocksCorner;
        
        
        public GameObject MiniBlockPrefab;
        public GameObject MiniBlocksPrefab;
        public GameObject MiniBlocksVertPrefab;
        public GameObject MiniBlocksCorner;
        
        public GameObject SmallBlockPrefab;
        public GameObject SmallBlocksPrefab;
        public GameObject SmallBlocksVertPrefab;
        public GameObject SmallBlocksCorner;
        
        public GameObject LargeBlockPrefab;
        public GameObject LargeBlocksPrefab;
        public GameObject LargeBlocksVertPrefab;
        public GameObject LargeBlocksCorner;

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

//        public void SpawnObject(SelectedObject selectedObject, SelectedSize selectedSize, Vector3 position, Quaternion rotation)
//        {
//            GameObject prefabToSpawn;
//            switch (selectedObject)
//            {
//                case SelectedObject.Blocks:
//                    prefabToSpawn = BlocksPrefab;
//                    break;
//                case SelectedObject.BlocksVertical:
//                    prefabToSpawn = BlocksVertPrefab;
//                    break;
//                case SelectedObject.BlocksCorner:
//                    prefabToSpawn = BlocksCorner;
//                    break;
//                default:
//                    prefabToSpawn = BlockPrefab;
//                    break;
//            }
//
//            float scale;
//            switch (selectedSize)
//            {
//                case SelectedSize.Mini:
//                    scale = 0.25f;
//                    break;
//                case SelectedSize.Small:
//                    scale = 0.5f;
//                    break;
//                case SelectedSize.Large:
//                    scale = 1.5f;
//                    break;
//                default:
//                    scale = 1f;
//                    break;
//            }
//
//            Vector3 tempScale = prefabToSpawn.transform.localScale;
//
//            tempScale.x = scale;
//            tempScale.y = scale;
//            tempScale.z = scale;
//
//            prefabToSpawn.transform.localScale = tempScale;
//            
//            CmdSpawnObject(prefabToSpawn, position, rotation);           
//        }
        
        
        //Quick hack to avoid chaning Prefab scale - as scale is not transferred over UNet
        public void SpawnObject(SelectedObject selectedObject, SelectedSize selectedSize, Vector3 position, Quaternion rotation)
        {
            CmdSpawnObject(selectedObject, selectedSize, position, rotation);           
        }
        
        [Command]
        public void CmdSpawnObject(SelectedObject selectedObject, SelectedSize selectedSize, Vector3 position, Quaternion rotation)
        {
            _ShowAndroidToastMessage("In Command; obj: " +selectedObject + ", size:" + selectedSize);
            
            GameObject prefabToSpawn;
            if (selectedSize == SelectedSize.Mini)
            {
                switch (selectedObject)
                {
                    case SelectedObject.Blocks:
                        prefabToSpawn = MiniBlocksPrefab;
                        break;
                    case SelectedObject.BlocksVertical:
                        prefabToSpawn = MiniBlocksVertPrefab;
                        break;
                    case SelectedObject.BlocksCorner:
                        prefabToSpawn = MiniBlocksCorner;
                        break;
                    default:
                        prefabToSpawn = MiniBlockPrefab;
                        break;
                }    
            } else if (selectedSize == SelectedSize.Small)
            {
                switch (selectedObject)
                {
                    case SelectedObject.Blocks:
                        prefabToSpawn = SmallBlocksPrefab;
                        break;
                    case SelectedObject.BlocksVertical:
                        prefabToSpawn = SmallBlocksVertPrefab;
                        break;
                    case SelectedObject.BlocksCorner:
                        prefabToSpawn = SmallBlocksCorner;
                        break;
                    default:
                        prefabToSpawn = SmallBlockPrefab;
                        break;
                }        
            } else if (selectedSize == SelectedSize.Large)
            {
                switch (selectedObject)
                {
                    case SelectedObject.Blocks:
                        prefabToSpawn = LargeBlocksPrefab;
                        break;
                    case SelectedObject.BlocksVertical:
                        prefabToSpawn = LargeBlocksVertPrefab;
                        break;
                    case SelectedObject.BlocksCorner:
                        prefabToSpawn = LargeBlocksCorner;
                        break;
                    default:
                        prefabToSpawn = LargeBlockPrefab;
                        break;
                }        
            }
            else
            {
                switch (selectedObject)
                {
                    case SelectedObject.Blocks:
                        prefabToSpawn = BlocksPrefab;
                        break;
                    case SelectedObject.BlocksVertical:
                        prefabToSpawn = BlocksVertPrefab;
                        break;
                    case SelectedObject.BlocksCorner:
                        prefabToSpawn = BlocksCorner;
                        break;
                    default:
                        prefabToSpawn = BlockPrefab;
                        break;
                }
            }
                        
            
            bool prefab = prefabToSpawn == null;
            _ShowAndroidToastMessage("In Command; prefab = null: " + prefab + ", posi:" + position);
           
            var blockObject = Instantiate(prefabToSpawn, position, rotation);
            
            bool obj = blockObject == null;
            _ShowAndroidToastMessage("In Command; block = null:  " + obj);

            // Spawn the object in all clients.
            NetworkServer.Spawn(blockObject);

        }

//        [ClientRpc]
//        void RpcSpawn(GameObject prefabToSpawn)
//        {
//            Spawn()
//        }

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
        
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject =
                        toastClass.CallStatic<AndroidJavaObject>(
                            "makeText", unityActivity, message, 0);
                    toastObject.Call("show");
                }));
            }
        }
    }
}
