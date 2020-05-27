using UnityEngine;
using UnityEngine.UI;

namespace GoogleARCore.Examples.CloudAnchors
{
    public class ObjectSelectionController : MonoBehaviour
    {

        void Start()
        {
            
        }
      
        public void OnButtonClicked(Button btn)
        {
            btn.interactable = false;
        }

        private void Update()
        {
            
        }
    }
}