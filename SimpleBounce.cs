using UnityEngine;
using Random = UnityEngine.Random;

namespace FakeMG.Framework
{
    public class SimpleBounce : MonoBehaviour
    {
        [SerializeField] private float _shootForce = 5f;
        [SerializeField] private float _gravity = -15f;
        [SerializeField] private int _bounceCount = 2;
        [SerializeField] private float _groundY = 0.5f;
        [SerializeField] private float _velocityXZDamping = 0.5f;
        [SerializeField] private float _velocityXZMultiplier = 0.7f;

        private Vector3 _velocity;
        private int _currentBounces;
        private bool _isFalling = true;

        private void Update()
        {
            if (_isFalling)
            {
                _velocity += new Vector3(0, _gravity * Time.deltaTime, 0);

                transform.position += _velocity * Time.deltaTime;

                if (transform.position.y <= _groundY)
                {
                    BounceOrStop();
                }
            }
        }

        public void ShootObject()
        {
            Vector3 randomDirection =
                new Vector3(Random.Range(-1f, 1f), Random.Range(0.8f, 1f), Random.Range(-1f, 1f))
                    .normalized;
            _velocity = randomDirection * _shootForce;
            _velocity.x *= _velocityXZMultiplier;
            _velocity.z *= _velocityXZMultiplier;
            _isFalling = true;
            _currentBounces = 0;
        }

        private void BounceOrStop()
        {
            if (_currentBounces < _bounceCount)
            {
                // Bounce logic: reverse Y velocity and reduce its magnitude
                _velocity.y = -_velocity.y * 0.5f;
                _velocity.x *= _velocityXZDamping;
                _velocity.z *= _velocityXZDamping;

                _currentBounces++;

                // Ensure the object stays above the ground
                transform.position = new Vector3(transform.position.x, _groundY + 0.01f, transform.position.z);
            }
            else
            {
                // Stop the object
                _isFalling = false;
                _currentBounces = 0;
                _velocity = Vector3.zero;
                transform.position = new Vector3(transform.position.x, _groundY, transform.position.z);
            }
        }
    }
}