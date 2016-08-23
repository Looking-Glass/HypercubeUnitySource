using UnityEngine;
using System.Collections;

//inherit from this class to automatically receive touch events from the hypercube
//alternatively, you can foreach loop on input.frontTouchScreen.touches or input.backTouchScreen.touches

namespace hypercube
{

    public class touchscreenTarget : MonoBehaviour
    {

        public float leftBorder = 0f;
        public float rightBorder = 1f;
        public float topBorder = 1f;
        public float bottomBorder = 0f;

        void OnEnable()
        {

        }


        public virtual void onTouchDown(touch touch)
        {
        }

        public virtual void onTouchUp(touch touch)
        {
        }

        public virtual void onTouchMoved(touch touch)
        {
        }



        public Vector2 mapToRange(float x, float y)
        {
            Vector2 position = new Vector2(x, y);
            position.x = map(position.x, 0, 1.0f, leftBorder, rightBorder);
            position.y = map(position.y, 0.0f, 1.0f, bottomBorder, topBorder);
            return position;
        }

        public static float map(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }
    }

}