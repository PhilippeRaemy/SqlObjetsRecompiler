namespace SqlObjetsRecompiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    static class ArgumentsExtensions
    {
        public static IEnumerable<string> GetArgumentValue(this IEnumerable<string> args, string arg, Action<string> valueSetter)
            => args.GetArgumentValue(arg, valueSetter, null);

        public static IEnumerable<string> GetSwitchValue(this IEnumerable<string> args, string arg, Action switchSetter)
            => args.GetArgumentValue(arg, null, switchSetter);

        public static IEnumerable<string> GetArgumentValue(this IEnumerable<string> args, string arg, Action<string> valueSetter, Action switchSetter)
        {
            var argArray = args as string[] ?? args.ToArray();
            if (valueSetter == null && switchSetter != null)
            {
                if (argArray.Any(a => string.Equals(a, arg, StringComparison.InvariantCultureIgnoreCase)))
                {
                    switchSetter();
                    return argArray
                        .Where(a => !string.Equals(a, arg, StringComparison.InvariantCultureIgnoreCase));
                }
            }
            else if (valueSetter != null)
            {
                var value = argArray
                    .SkipWhile(a => !string.Equals(a, arg, StringComparison.InvariantCultureIgnoreCase))
                    .Skip(1)
                    .FirstOrDefault();
                valueSetter(value);
                if (value != null) switchSetter?.Invoke();
                return argArray.TakeWhile(a => !string.Equals(a, arg, StringComparison.InvariantCultureIgnoreCase))
                    .Concat(argArray.SkipWhile(a => !string.Equals(a, arg, StringComparison.InvariantCultureIgnoreCase)).Skip(2));
            }

            return argArray;
        }
    }
}