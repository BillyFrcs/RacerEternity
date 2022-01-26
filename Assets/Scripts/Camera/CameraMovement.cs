using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CameraGame
{
    public class CameraMovement : MonoBehaviour
    {
        private float _movementSpeed = 25.0f;
        private float _vertical;
        private float _horizontal;

        // Update is called once per frame
        private void Update()
        {
            // 1
            var xValue = Input.GetAxis("Horizontal") * _movementSpeed * Time.deltaTime;
            var yValue = Input.GetAxis("Jump") * _movementSpeed * Time.deltaTime;
            var zValue = Input.GetAxis("Vertical") * _movementSpeed * Time.deltaTime;

            transform.Translate(new Vector3(xValue, yValue, zValue));

            /*
            // 2
            _vertical = Input.GetAxisRaw("Vertical") * _movementSpeed;
            _horizontal = Input.GetAxisRaw("Horizontal") * _movementSpeed;
    
            _vertical += Time.deltaTime;
            _horizontal += Time.deltaTime;
            
            transform.Translate(new Vector3(_horizontal, _vertical, 0f));
            */
        }
    }
}
