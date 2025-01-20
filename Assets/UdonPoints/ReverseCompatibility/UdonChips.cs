using UdonPoints;
using UdonSharp;

#if REVERSE_UC
namespace UCS
#else
namespace UdonPoints.ReverseCompatibility
#endif
{
    public class UdonChips : UdonSharpBehaviour
    {
        public UdonPointsManager Manager;
        public UdonPointsBehaviour Target;
        
        public float money {
            get => Manager.GetMoney(Target).MoneyToFloat();
            set => Manager.EffectMoney(MoneyAction.Set, value, Target);
        }

        public string format = "$ {0:F0}";
    }
}