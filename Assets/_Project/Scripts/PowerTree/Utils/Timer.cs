namespace PowerTree
{
// Helper class for node timers
    public class Timer
    {
        public float Duration { get; set; }
        public float CurrentTime { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsCompleted => !IsRunning && CurrentTime <= 0;
        
        public event System.Action OnTimerCompleted;
        
        public Timer(float duration)
        {
            Duration = duration;
            CurrentTime = 0;
            IsRunning = false;
        }
        
        public void Start()
        {
            CurrentTime = Duration;
            IsRunning = true;
        }
        
        public void Stop()
        {
            IsRunning = false;
        }
        
        public void Reset()
        {
            CurrentTime = Duration;
        }
        
        public void Update(float deltaTime)
        {
            if (!IsRunning) return;
            
            if (CurrentTime > 0)
            {
                CurrentTime -= deltaTime;
                if (CurrentTime <= 0)
                {
                    IsRunning = false;
                    OnTimerCompleted?.Invoke();
                }
            }
        }
    }
}