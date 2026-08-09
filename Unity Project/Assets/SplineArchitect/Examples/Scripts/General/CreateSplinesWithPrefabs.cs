using UnityEngine;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/CreateSplinesWithPrefabs")]
    public class CreateSplinesWithPrefabs : MonoBehaviour
    {
        public GameObject prefab;
        public int amount;
        public float spawnDelay;
        public float distanceBetween;

        private float timer;
        private int i;

        void Update()
        {
            if (i > amount)
                return;

            if(timer > spawnDelay)
            {
                timer = 0;

                Vector3 position = new Vector3(0, 0, i * distanceBetween - 50);
                Quaternion rotation = i % 2 == 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
                Instantiate(prefab, position, rotation);

                i++;
            }

            timer += Time.deltaTime;
        }
    }
}
