namespace OSK
{
    public interface IUpdateable
    {
        void OnUpdate();
    }

    public interface IFixedUpdateable
    {
        void OnFixedUpdate();
    }

    public interface ILateUpdateable
    {
        void OnLateUpdate();
    }
}
