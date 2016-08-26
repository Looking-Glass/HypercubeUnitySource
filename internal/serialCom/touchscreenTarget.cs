using UnityEngine;
using System.Collections;

//inherit from this class to automatically receive touch events from the hypercube
//alternatively, you can foreach loop on input.frontTouchScreen.touches or input.backTouchScreen.touches

    public class touchscreenTarget : MonoBehaviour
    {
        void OnEnable()
        {
            hypercube.input._setTouchScreenTarget(this, true);
        }
        void OnDisable()
        {
            hypercube.input._setTouchScreenTarget(this, false);
        }
        void OnDestroy()
        {
            hypercube.input._setTouchScreenTarget(this, false);
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
