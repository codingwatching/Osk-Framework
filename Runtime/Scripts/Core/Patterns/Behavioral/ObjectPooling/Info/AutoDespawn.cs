using UnityEngine;


namespace OSK
{
    public class AutoDespawn : MonoBehaviour, IPoolable, IUpdateable
    {
        public float lifeTime = 2.0f;
        public bool isUsePool;
        private float _timer;
        public float TimeLeft => _timer;

        public void OnSpawn()
        {
            _timer = lifeTime;
            Main.RegisterTick(this);
        }

        public void OnDespawn()
        {
            Main.UnRegisterTick(this);
        }

        public void OnUpdate()
        {
            if (_timer > 0)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    DespawnSelf();
                }
            }
        }

        private void DespawnSelf()
        {
            if (isUsePool)
                Main.Pool.Despawn(this);
            else
                Destroy(gameObject);
        }
    }
}