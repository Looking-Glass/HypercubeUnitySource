using UnityEngine;
using System.Collections;

    public class hypercubeTouchControl : MonoBehaviour
    {
        public float sensitivity = 1f;
        public bool allowTwist = true;

        void Update()
        {
            if (hypercube.input.frontScreen == null) //Volume not connected via USB, or not yet init
                return;  

            Vector2 average = hypercube.input.frontScreen.averageDiff;

            if (average == Vector2.zero)
                return;
 
            transform.Rotate(0f, average.x * sensitivity * 180f, 0f, Space.World);
         
            if (allowTwist)
                transform.Rotate(-average.y * sensitivity * 180f, 0f, hypercube.input.frontScreen.twist, Space.Self);
            else
                transform.Rotate(-average.y * sensitivity * 180f, 0f, 0f, Space.Self);

            transform.localScale *= 1f / hypercube.input.frontScreen.pinch;
        }

    }

