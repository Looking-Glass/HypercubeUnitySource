using UnityEngine;
using System.Collections;

//inherit from this class to automatically receive touch events from the hypercube
//alternatively, you can foreach loop on input.frontTouchScreen.touches or input.backTouchScreen.touches

namespace hypercube
{
    public class touchScreenTarget : MonoBehaviour
    {
        void OnEnable()
        {
            input._setTouchScreenTarget(this, true);
        }
        void OnDisable()
        {
            input._setTouchScreenTarget(this, false);
        }
        void OnDestroy()
        {
            input._setTouchScreenTarget(this, false);
        }

        public virtual void onTouchDown(hypercube.touch touch)
        {
        }

        public virtual void onTouchUp(hypercube.touch touch)
        {
        }

        public virtual void onTouchMoved(hypercube.touch touch)
        {
        }
    }
}
