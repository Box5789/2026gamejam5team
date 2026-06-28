using UnityEngine;

namespace KimbapGame.Order
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Order/Person Impact Responder")]
    public sealed class OrderPersonImpactResponder : MonoBehaviour
    {
        [SerializeField]
        private OrderSceneController controller;

        [SerializeField] private GameObject Particle;
        [SerializeField] private GameObject Particle2;
        public OrderSceneController Controller => controller;

        public void Configure(OrderSceneController sceneController)
        {
            controller = sceneController;
        }

        public bool NotifyImpact(float impactSpeed)
        {
            if (controller == null)
            {
                controller = FindObjectOfType<OrderSceneController>();
            }

            return controller != null && controller.TryPlayImpactReaction(impactSpeed);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            //Debug.Log(collision.relativeVelocity.magnitude);
            NotifyImpact(collision.relativeVelocity.magnitude);
            if (collision.relativeVelocity.magnitude > controller.impactReactionVelocityThreshold)
            {
                if(controller.currentOrder.orderImageName=="Order/사람_기본_1")
                    Instantiate(Particle, collision.contacts[0].point, Quaternion.identity);
                else
                    Instantiate(Particle2, collision.contacts[0].point, Quaternion.identity);
            }
                
        }
    }
}
