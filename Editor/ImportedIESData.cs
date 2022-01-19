using UnityEngine;

namespace Valax321.IESImporter
{
    public class ImportedIESData : ScriptableObject
    {
        [SerializeField] private AnimationCurve m_SamplesVertical;
        [SerializeField] private AnimationCurve m_SamplesHorizontal;
        [SerializeField] private float m_MaxIntensity;

        public AnimationCurve samplesVertical
        {
            get => m_SamplesVertical;
            set => m_SamplesVertical = value;
        }
        
        public AnimationCurve samplesHorizontal
        {
            get => m_SamplesHorizontal;
            set => m_SamplesHorizontal = value;
        }

        public float maxIntensity
        {
            get => m_MaxIntensity;
            set => m_MaxIntensity = value;
        }
    }
}