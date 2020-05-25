using UnityEngine;
using UnityEngine.SceneManagement;

namespace Character
{
    public class DeathBarrier : MonoBehaviour
    {
        public LevelChanger levelChanger;
    
        public float deathBarrierX = -2f;
        public float deathBarrierY = -2f;

        public bool checkX = false;
        public bool checkY = true;
    
        private void Update()
        {
            if (checkX && transform.position.x < deathBarrierX || checkY && transform.position.y < deathBarrierY)
            {
                ReloadLevel();
            }
        }

        public void ReloadLevel()
        {
            levelChanger.FadeToLevel(SceneManager.GetActiveScene().name);
        }
    }
}
