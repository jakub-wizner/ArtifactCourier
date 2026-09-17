using ArtifactCourier.Driving;
using ArtifactCourier.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ArtifactCourier.UI
{
    public sealed class DrivingHUD : MonoBehaviour
    {
        [SerializeField] private Text score;
        [SerializeField] private Text contract;
        [SerializeField] private Text toast;
        [SerializeField] private Text nitro;
        [SerializeField] private RectTransform fill;
        private CarController car;
        private DrivingStyle style;
        public void Configure(Text scoreLabel, Text contractLabel, Text toastLabel, Text nitroLabel, RectTransform bar)
        { score = scoreLabel; contract = contractLabel; toast = toastLabel; nitro = nitroLabel; fill = bar; }
        private void Start()
        { car = FindFirstObjectByType<CarController>(); style = FindFirstObjectByType<DrivingStyle>(); }
        private void Update()
        {
            if (car == null || style == null) return;
            score.text = $"STYLE {style.Score:00000}   ×{style.Combo}   BEST {style.Best:00000}";
            contract.text = style.Challenge;
            toast.text = style.Toast;
            nitro.text = car.IsBoosting ? "NITRO ACTIVE" : $"SHIFT  NITRO {car.Nitro * 100f:0}%   |   CTRL  DRIFT";
            fill.anchorMax = new Vector2(car.Nitro, 1f);
        }
    }
}
