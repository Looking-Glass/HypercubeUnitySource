/*  
Hypercube: Volume Plugin is released under the MIT License:

Copyright 2016 Looking Glass Factory, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy 
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
of the Software, and to permit persons to whom the Software is furnished to do 
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all 
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE 
OR OTHER DEALINGS IN THE SOFTWARE.
*/

using UnityEngine;
using System.Collections;

    public class hypercubeTouchControl : MonoBehaviour
    {
        public float sensitivity = 1f;
        public bool allowTwist = true;

        void Update()
        {
            if (hypercube.input.touchPanel == null) //Volume not connected via USB, or not yet init
                return;  

            Vector2 average = hypercube.input.touchPanel.averageDiff;

            if (average == Vector2.zero)
                return;
 
            transform.Rotate(0f, average.x * sensitivity * 180f, 0f, Space.World);
         
            if (allowTwist)
                transform.Rotate(-average.y * sensitivity * 180f, 0f, hypercube.input.touchPanel.twist, Space.Self);
            else
                transform.Rotate(-average.y * sensitivity * 180f, 0f, 0f, Space.Self);

            transform.localScale *= 1f / hypercube.input.touchPanel.pinch;
        }

    }

