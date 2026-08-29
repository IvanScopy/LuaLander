using System;
using UnityEngine;

public class Lander : MonoBehaviour
{

    public static Lander Instance { get; private set; }

    private const float GRAVITY_NORMAL = 0.7f;

    public event EventHandler OnUpForce; 
    public event EventHandler OnLeftForce; 
    public event EventHandler OnRightForce; 
    public event EventHandler OnBeforForce;
    public event EventHandler OnCoinPickup;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;

    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }
    public event EventHandler<OnLanedEventArgs> OnLanded;

    public class OnLanedEventArgs : EventArgs
    {
        public LandingType landingType;
        public int score;
        public float dotVector;
        public float landingSpeed;
        public float scoreMult;

    }

    public enum LandingType
    {
        Success,
        WrongLandingArea,
        TooSteepAngle,
        TooFastLanding,
    }
    
    public enum State
    {
        WaitingToStart,
        Normal,
        GameOver,
    }

    private Rigidbody2D landerRigidbody2D;
    private float fuelAmount;
    private float fuelAmountMax = 10f;
    private State state;

    private void Awake()
    {
        Instance = this;
        fuelAmount = fuelAmountMax;
        state = State.WaitingToStart;

        landerRigidbody2D = GetComponent<Rigidbody2D>();
        landerRigidbody2D.gravityScale = 0f;
    }

    private void FixedUpdate()
    {
        OnBeforForce?.Invoke(this, EventArgs.Empty);

        switch (state)
        {
            default:
            case State.WaitingToStart:
                if (GameInput.Instance.IsUpActionPressed() || GameInput.Instance.IsLeftActionPressed()  || GameInput.Instance.IsRightActionPressed() )
                {
                    // any control
                    landerRigidbody2D.gravityScale = GRAVITY_NORMAL;
                    state = State.Normal;
                    SetState(State.Normal);
                }
                break;
            case State.Normal:
                // Debug.Log(fuelAmount);
                if (fuelAmount <=0f)
                {
                    return;
                }
                if (GameInput.Instance.IsUpActionPressed()  || GameInput.Instance.IsLeftActionPressed()  || GameInput.Instance.IsRightActionPressed() )
                {
                    // any control
                    ConsumeFuel();
                }

                if (GameInput.Instance.IsUpActionPressed() )
                {
                    float speed = 700f;
                    landerRigidbody2D.AddForce(speed * transform.up * Time.deltaTime);
                    OnUpForce?.Invoke(this, EventArgs.Empty);
                }

                if (GameInput.Instance.IsLeftActionPressed() )
                {
                    float turnSpeed = +100f;
                    landerRigidbody2D.AddTorque(turnSpeed * Time.deltaTime);
                    OnLeftForce?.Invoke(this, EventArgs.Empty);
                }

                if (GameInput.Instance.IsRightActionPressed() )
                {
                    float turnSpeed = -100f;
                    landerRigidbody2D.AddTorque(turnSpeed * Time.deltaTime);
                    OnRightForce?.Invoke(this, EventArgs.Empty);
                }
                break;
            case State.GameOver:
                break;
                    }
        
    }

    private void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (!collision2D.gameObject.TryGetComponent(out LandingPad landingPad))
        {
            Debug.Log("Crashed on the Terrain!");
            OnLanded?.Invoke(this, new OnLanedEventArgs
        {
            landingType = LandingType.WrongLandingArea,
            dotVector = 0f,
            score = 0,
            landingSpeed = 0f,
            scoreMult = 0
        });
            SetState(State.GameOver);
            return;
        }
        float softLandingVelocityMagnitude = 4f;
        float relativeVelocityMagnitude = collision2D.relativeVelocity.magnitude;
        if (relativeVelocityMagnitude > softLandingVelocityMagnitude)
        {
            // landed too hard
            Debug.Log("Landed too hard!");
            OnLanded?.Invoke(this, new OnLanedEventArgs
        {
            landingType = LandingType.TooFastLanding,
            dotVector = 0f,
            score = 0,
            landingSpeed = relativeVelocityMagnitude,
            scoreMult = 0
        });
            SetState(State.GameOver);
            return;
        }

        float dotVector = Vector2.Dot(Vector2.up, transform.up);
        float minDotVector = .90f;
        if (dotVector < minDotVector)
        {
            //bi nghieng 
            Debug.Log("Landed on a too steep angle");
            OnLanded?.Invoke(this, new OnLanedEventArgs
        {
            landingType = LandingType.TooSteepAngle,
            dotVector = dotVector,
            score = 0,
            landingSpeed = relativeVelocityMagnitude,
            scoreMult = 0
        });
            SetState(State.GameOver);
            return;
        }

        Debug.Log("Landed succsessful");

        float maxScoreAmountLandingAngle = 100;
        float scoreDotVectorMultiplier = 10f;
        float landingAngleScore = maxScoreAmountLandingAngle - Mathf.Abs(dotVector -1f) * scoreDotVectorMultiplier * maxScoreAmountLandingAngle ;

        float maxScoreAmountLandingSpeed = 100;
        float landingSpeedScore = (softLandingVelocityMagnitude - relativeVelocityMagnitude) * maxScoreAmountLandingSpeed;

        Debug.Log("landingAngleScore: " + landingAngleScore);
        Debug.Log("landingSpeedScore: " + landingSpeedScore);

        int score =Mathf.RoundToInt((landingSpeedScore + landingAngleScore) * landingPad.GetScoreMultiplier());

        Debug.Log("Score: " + score);
        OnLanded?.Invoke(this, new OnLanedEventArgs
        {
            landingType = LandingType.Success,
            dotVector = dotVector,
            score = score,
            landingSpeed = relativeVelocityMagnitude,
            scoreMult = landingPad.GetScoreMultiplier()
        });
        SetState(State.GameOver);

    }

    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.gameObject.TryGetComponent(out FuelPickup fuelPickup))
        {
            float addFuelAmount = 10f;
            fuelAmount = Mathf.Min(fuelAmount + addFuelAmount, fuelAmountMax);
            fuelPickup.DestroySelf();
        }

        if (collider2D.gameObject.TryGetComponent(out CoinPickup coinPickup))
        {
            OnCoinPickup?.Invoke(this, EventArgs.Empty);
            coinPickup.DestroySelf();
        }
    }

    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
        {
            state = state,
        });
    }

    private void ConsumeFuel()
    {
        float fuelConsumptionAmount = 1f;
        fuelAmount -= fuelConsumptionAmount * Time.deltaTime;
    }

    public float GetSpeedX()
    {
        return landerRigidbody2D.linearVelocityX;
    }

    public float GetSpeedY()
    {
        return landerRigidbody2D.linearVelocityY;
    }

    public float GetFuel()
    {
        return fuelAmount;
    }

    public float GetFuelNormalized()
    {
        return fuelAmount/fuelAmountMax;
    }

    
}
