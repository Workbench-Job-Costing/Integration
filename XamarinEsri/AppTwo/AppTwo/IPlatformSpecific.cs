using System;
using System.Collections.Generic;
using System.Text;

namespace AppTwo
{
    public interface IPlatformSpecific
    {
        /// <summary>
        /// See more about upper limits here: https://stackoverflow.com/questions/28729955/max-size-of-string-data-that-can-be-passed-in-intents?lq=1 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="componentName">Only component name as Xamarin has a single activity only</param>
        /// <param name="parameters">Upper limit is around 500kb</param>
        void StartActivity<T>(string componentName, T parameters);

        T GetParameter<T>();
    }
}
