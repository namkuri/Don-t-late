using System.Reflection;

namespace DontLate.Tests
{
    /// <summary>
    /// EditMode 테스트 공용 리플렉션 헬퍼 (S-024).
    /// 매니저는 [SerializeField] private 주입·private 메서드라 리플렉션으로 접근한다
    /// (에디터 모드에선 Awake/OnEnable이 안 돌므로 싱글톤·이벤트 간섭 없음).
    /// </summary>
    internal static class TestSupport
    {
        private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, FLAGS);
            if (field == null)
                throw new System.MissingFieldException(target.GetType().Name, fieldName);
            field.SetValue(target, value);
        }

        public static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, FLAGS);
            if (method == null)
                throw new System.MissingMethodException(target.GetType().Name, methodName);
            return method.Invoke(target, args);
        }
    }
}
