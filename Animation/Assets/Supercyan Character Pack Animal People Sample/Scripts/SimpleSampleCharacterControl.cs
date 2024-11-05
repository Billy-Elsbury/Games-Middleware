using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Supercyan.AnimalPeopleSample
{
    public class SimpleSampleCharacterControl : MonoBehaviour
    {
        public Transform rhg;
        private enum ControlMode
        {
            /// <summary>
            /// Up moves the character forward, left and right turn the character gradually and down moves the character backwards
            /// </summary>
            Tank,
            /// <summary>
            /// Character freely moves in the chosen direction from the perspective of the camera
            /// </summary>
            Direct
        }

        [SerializeField] private float m_moveSpeed = 2;
        [SerializeField] private float m_turnSpeed = 200;
        [SerializeField] private float m_jumpForce = 4;

        [SerializeField] private Animator m_animator = null;
        [SerializeField] private Rigidbody m_rigidBody = null;

        [SerializeField] private ControlMode m_controlMode = ControlMode.Direct;

        private float m_currentV = 0;
        private float m_currentH = 0;

        private readonly float m_interpolation = 10;
        private readonly float m_walkScale = 0.33f;
        private readonly float m_backwardsWalkScale = 0.16f;
        private readonly float m_backwardRunScale = 0.66f;

        private bool m_wasGrounded;
        private Vector3 m_currentDirection = Vector3.zero;

        private float m_jumpTimeStamp = 0;
        private float m_minJumpInterval = 0.25f;
        private bool m_jumpInput = false;

        private bool m_isGrounded;

        private List<Collider> m_collisions = new List<Collider>();

        ObjectScript focusObject;

        List<ObjectScript> allCatchableItems;


        private void Awake()
        {
            if (!m_animator) { gameObject.GetComponent<Animator>(); }
            if (!m_rigidBody) { gameObject.GetComponent<Animator>(); }

            allCatchableItems = FindObjectsOfType<ObjectScript>().ToList();
        }

        private ObjectScript ClosestObject()
        {
            ObjectScript closestObject = null;
            float closestDistance = float.MaxValue;

            foreach (ObjectScript obj in allCatchableItems)
            {
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                }
            }
            return closestObject;
        }

            private void OnCollisionEnter(Collision collision)
        {
            ContactPoint[] contactPoints = collision.contacts;
            for (int i = 0; i < contactPoints.Length; i++)
            {
                if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
                {
                    if (!m_collisions.Contains(collision.collider))
                    {
                        m_collisions.Add(collision.collider);
                    }
                    m_isGrounded = true;
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            ContactPoint[] contactPoints = collision.contacts;
            bool validSurfaceNormal = false;
            for (int i = 0; i < contactPoints.Length; i++)
            {
                if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
                {
                    validSurfaceNormal = true; break;
                }
            }

            if (validSurfaceNormal)
            {
                m_isGrounded = true;
                if (!m_collisions.Contains(collision.collider))
                {
                    m_collisions.Add(collision.collider);
                }
            }
            else
            {
                if (m_collisions.Contains(collision.collider))
                {
                    m_collisions.Remove(collision.collider);
                }
                if (m_collisions.Count == 0) { m_isGrounded = false; }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (m_collisions.Contains(collision.collider))
            {
                m_collisions.Remove(collision.collider);
            }
            if (m_collisions.Count == 0) { m_isGrounded = false; }
        }

        private bool isPickingUp = false;
        private bool isHoldingObject = false;
        private Vector3 ikTargetPosition;
        private Quaternion ikTargetRotation;

        private float maxReachDistance = 1.5f; 
        private float reachSpeed = 3f; 

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F) && !isHoldingObject)
            {
                focusObject = ClosestObject();
            }

            if (Input.GetKeyDown(KeyCode.E) && focusObject != null && !isHoldingObject)
            {
                isPickingUp = true;
                m_animator.SetTrigger("PickUp");
                ikTargetPosition = m_animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            }

            if (!m_jumpInput && Input.GetKey(KeyCode.Space))
            {
                m_jumpInput = true;
            }

            if (Input.GetKeyDown(KeyCode.T) && isHoldingObject)
            {
                ThrowObject();
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (isPickingUp && focusObject != null)
            {
                Transform handTransform = rhg;

                Vector3 directionToObject = (focusObject.transform.position - transform.position).normalized;
                float distanceToObject = Vector3.Distance(transform.position, focusObject.transform.position);

                float clampedDistance = Mathf.Min(distanceToObject, maxReachDistance);

                Vector3 targetPosition = transform.position + directionToObject * clampedDistance;

                // Lerp hand to target
                ikTargetPosition = Vector3.Lerp(ikTargetPosition, targetPosition, Time.deltaTime * reachSpeed);
                ikTargetRotation = Quaternion.LookRotation(focusObject.transform.position - handTransform.position);

                m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                m_animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                m_animator.SetIKPosition(AvatarIKGoal.RightHand, ikTargetPosition);
                m_animator.SetIKRotation(AvatarIKGoal.RightHand, ikTargetRotation);
                
                float distanceToHand = Vector3.Distance(handTransform.position, focusObject.transform.position) - focusObject.transform.localScale.x;
                if (distanceToHand <= 0.4f)
                {
                    AttachObjectToHand(focusObject);
                    isPickingUp = false;
                }
            }
            else
            {
                // Reset IK weights
                m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                m_animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            }
        }


        private void AttachObjectToHand(ObjectScript obj)
        {
            obj.transform.SetParent(rhg);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            Rigidbody objRigidbody = obj.GetComponent<Rigidbody>();
            if (objRigidbody != null)
            {
                objRigidbody.isKinematic = true;
            }

            Collider objCollider = obj.GetComponent<Collider>();
            if (objCollider != null)
            {
                objCollider.enabled = false;
            }

            isHoldingObject = true;
            
        }


        private void ThrowObject()
        {
            if (isHoldingObject && focusObject != null)
            {
                focusObject.transform.SetParent(null);

                Rigidbody objRigidbody = focusObject.GetComponent<Rigidbody>();
                if (objRigidbody != null)
                {
                    objRigidbody.isKinematic = false;

                    objRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

                    Vector3 throwDirection = transform.forward + Vector3.up * 0.6f;
                    float throwForce = 10f;
                    objRigidbody.AddForce(throwDirection * throwForce, ForceMode.Impulse);
                }

                Collider objCollider = focusObject.GetComponent<Collider>();
                if (objCollider != null)
                {
                    objCollider.enabled = true;
                }

                isHoldingObject = false;
                focusObject = null;

                m_animator.SetTrigger("Throw");
            }
        }


        private void FixedUpdate()
        {
            m_animator.SetBool("Grounded", m_isGrounded);
            if(Input.GetKeyDown(KeyCode.J))
            m_animator.SetTrigger("Wave");
            switch (m_controlMode)
            {
                case ControlMode.Direct:
                    DirectUpdate();
                    break;

                case ControlMode.Tank:
                    TankUpdate();
                    break;

                default:
                    Debug.LogError("Unsupported state");
                    break;
            }

            m_wasGrounded = m_isGrounded;
            m_jumpInput = false;
        }

        private void TankUpdate()
        {
            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");

            bool walk = Input.GetKey(KeyCode.LeftShift);

            if (v < 0)
            {
                if (walk) { v *= m_backwardsWalkScale; }
                else { v *= m_backwardRunScale; }
            }
            else if (walk)
            {
                v *= m_walkScale;
            }

            m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
            m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

            transform.position += transform.forward * m_currentV * m_moveSpeed * Time.deltaTime;
            transform.Rotate(0, m_currentH * m_turnSpeed * Time.deltaTime, 0);

            m_animator.SetFloat("MoveSpeed", m_currentV);

            JumpingAndLanding();
        }

        private void DirectUpdate()
        {
            float v = Input.GetAxis("Vertical");
            float h = Input.GetAxis("Horizontal");

            Transform camera = Camera.main.transform;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                v *= m_walkScale;
                h *= m_walkScale;
            }

            if(Input.GetKey(KeyCode.Mouse0))
            {
            }

            m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
            m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

            Vector3 direction = camera.forward * m_currentV + camera.right * m_currentH;

            float directionLength = direction.magnitude;
            direction.y = 0;
            direction = direction.normalized * directionLength;

            if (direction != Vector3.zero)
            {
                m_currentDirection = Vector3.Slerp(m_currentDirection, direction, Time.deltaTime * m_interpolation);

                transform.rotation = Quaternion.LookRotation(m_currentDirection);
                transform.position += m_currentDirection * m_moveSpeed * Time.deltaTime;

                m_animator.SetFloat("MoveSpeed", direction.magnitude);
            }

            JumpingAndLanding();
        }

        private void JumpingAndLanding()
        {
            bool jumpCooldownOver = (Time.time - m_jumpTimeStamp) >= m_minJumpInterval;

            if (jumpCooldownOver && m_isGrounded && m_jumpInput)
            {
                m_jumpTimeStamp = Time.time;
                m_rigidBody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
            }
        }

        private void LateUpdate()
        {
           
        if (focusObject)
            TurnHeadTo(focusObject.transform);

        }

        private void TurnHeadTo(Transform targetTransform)
        {
            Transform headTransform = m_animator.GetBoneTransform(HumanBodyBones.Head);
            
            headTransform.LookAt(targetTransform);
        }
    }
}