using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class WaterSpring : MonoBehaviour
{
    [SerializeField]
    private int waveIndex = 0;
    [SerializeField]
    private SpriteShapeController spriteShapeController = null;
    /////////////////
    [System.NonSerialized]
    public float velocity = 0;
    private float force = 0;
    // current height
    [System.NonSerialized]
    public float height = 0f;
    // normal height
    private float target_height = 0f;
    private float resistance = 40f;
    public void Init(SpriteShapeController ssc)
    {
        var index = transform.GetSiblingIndex();
        waveIndex = index + 1;
        spriteShapeController = ssc;

        velocity = 0;
        height = transform.localPosition.y;
        target_height = transform.localPosition.y;
        name = "spring_" + waveIndex.ToString();

        GetComponent<Collider2D>().forceSendLayers = 0; // 성능을 위해 OnCollisionEnter2D 을 사용해서 충돌처리를 1회만 한다. 
    }
    public void WaveSpringUpdate(float springStiffness, float dampening)
    {
        height = transform.localPosition.y;
        // maximum extension
        var x = height - target_height;
        var loss = -dampening * velocity;

        force = -springStiffness * x + loss;
        velocity += force;
        var y = transform.localPosition.y;
        transform.localPosition = new Vector3(transform.localPosition.x, y + velocity, transform.localPosition.z);

    }
    public void WavePointUpdate()
    {
        if (spriteShapeController != null)
        {
            Spline waterSpline = spriteShapeController.spline;
            try
            {
                Vector3 wavePosition = waterSpline.GetPosition(waveIndex);
                waterSpline.SetPosition(waveIndex, new Vector3(wavePosition.x, transform.localPosition.y, wavePosition.z));
            }
            catch
            {
                Destroy(gameObject);
            }
        }
    }
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag.Equals("FallingObject"))
        {
            FallingObject fallingObject = other.gameObject.GetComponent<FallingObject>();
            Rigidbody2D rb = fallingObject.rigidbody2D;
            Vector3 speed = rb.velocity;
            float kineticEnergy = 0.5f * (rb.mass * speed.magnitude);

            velocity += kineticEnergy / resistance;
        }
    }
}