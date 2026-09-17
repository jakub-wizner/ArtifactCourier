using ArtifactCourier.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ArtifactCourier.UI
{
    public sealed class GarageSelection : MonoBehaviour
    {
        [SerializeField] Button[] choices;
        public void Configure(Button[] buttons){choices=buttons;}
        void Start(){for(int i=0;i<choices.Length;i++){int choice=i;choices[i].onClick.AddListener(()=>Select(choice));}Select(GameSession.SelectedVehicle);}
        void Select(int choice)
        {
            GameSession.SelectedVehicle=choice;
            string[] names={"NIGHTJAR","BISON","KESTREL"};
            for(int i=0;i<choices.Length;i++)
            {
                choices[i].GetComponentInChildren<Text>().text=(i==choice?"SELECTED / ":"")+names[i];
                var colors=choices[i].colors;colors.normalColor=i==choice?new Color(.25f,.9f,.9f):Color.white;choices[i].colors=colors;
            }
        }
    }
}
