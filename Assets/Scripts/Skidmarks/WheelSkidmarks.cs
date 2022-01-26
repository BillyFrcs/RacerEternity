using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Skidmark
{
    public class WheelSkidmarks : MonoBehaviour
    {
        // #pragma strict

        // @script RequireComponent(WheelCollider) //We need a wheel collider

        public GameObject skidCaller; //The parent object having a rigidbody attached to it.
       
        public float startSlipValue = 0.5f;

        private Skidmarks _skidmarks = null; //To hold the skidmarks object
       
        private int _lastSkidmark = 0; //To hold last skidmarks data

        private WheelCollider _wheelCol; //To hold self wheel collider

        // Start is called before the first frame update
        private void Start()
        {
            //Get the Wheel Collider attached to self
            skidCaller = transform.root.gameObject;

            _wheelCol = GetComponent<WheelCollider>();

            //find object "Skidmarks" from the game
            if (FindObjectOfType(typeof(Skidmarks)))
            {
                _skidmarks = FindObjectOfType(typeof(Skidmarks)) as Skidmarks;
            }
            else
            {
                Debug.Log("No skidmarks object found. Skidmarks will not be drawn");
            }
        }

        //This has to be in fixed update or it wont get time to make skidmesh fully.
        private void FixedUpdate()
        {
            // WheelHit GroundHit; //variable to store hit data

            // Improve performance
            _wheelCol.GetGroundHit(out WheelHit GroundHit); //store hit data into GroundHit

            var wheelSlipAmount = Mathf.Abs(GroundHit.sidewaysSlip);

            if (wheelSlipAmount > startSlipValue) //if sideways slip is more than desired value
            {
                /*Calculate skid point:
                Since the body moves very fast, the skidmarks would appear away from the wheels because by the time the
                skidmarks are made the body would have moved forward. So we multiply the rigidbody's velocity vector x 2 
                to get the correct position
                */

                const float INTENSITY = 0.0f; // Default 0.2f

                var skidPoint = GroundHit.point + INTENSITY * Time.fixedDeltaTime * skidCaller.GetComponent<Rigidbody>().velocity;

                //Add skidmark at the point using AddSkidMark function of the Skidmarks object
                //Syntax: AddSkidMark(Point, Normal, Intensity(max value 1), Last Skidmark index);

                _lastSkidmark = _skidmarks.AddSkidMark(skidPoint, GroundHit.normal, wheelSlipAmount / INTENSITY, _lastSkidmark);
            }
            
            /*
            else
            {
                //stop making skidmarks
                _lastSkidmark = -1;
            }
            */
        }
    }
}
