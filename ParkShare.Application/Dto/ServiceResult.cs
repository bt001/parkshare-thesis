namespace ParkShare.Application.Dto
{   /// <summary>
    /// JobResult with Payload
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class ServiceResult<T> : ServiceResult
        where T : notnull
    {
        /// <summary>
        /// Data Payload
        /// </summary>
        public T Payload { get; }

        /// <summary>
        /// Returns if a result is successful and the errors if any exist with payload
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="successful"></param>
        /// <param name="messages"></param>
        private ServiceResult(T payload, bool successful, List<string>? messages) : base(successful, messages)
            => Payload = payload;

        /// <summary>
        /// Returns the JobResult with a list of errors
        /// </summary>
        /// <param name="message"></param>
        /// <returns><see cref="ServiceResult"/></returns>
        public static new ServiceResult<T> WithError(string message) => new ServiceResult<T>(default!, false, new List<string> { message });

        /// <summary>
        /// Returns the JobResult with a list of errors
        /// </summary>
        /// <param name="messages"></param>
        /// <returns><see cref="ServiceResult"/></returns>
        public static new ServiceResult<T> WithErrors(List<string> messages) => new ServiceResult<T>(default!, false, messages);

        /// <summary>
        /// Returns the successful JobResult
        /// </summary>
        /// <returns><see cref="ServiceResult"/></returns>
        public static new ServiceResult<T> Failed => new ServiceResult<T>(default!, false, new List<string>());

        /// <summary>
        /// Returns the successful JobResult with payload
        /// </summary>
        /// <param name="payload"></param>
        /// <returns><see cref="ServiceResult{T}"/></returns>
        public static new ServiceResult<T> Success(T payload) => new ServiceResult<T>(payload, true, null);

        /// <summary>
        /// Returns the partially successful JobResult with payload and the errors that occured
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="errors"></param>
        /// <returns><see cref="ServiceResult{T}"/></returns>
        public static ServiceResult<T> PartialSuccess(T payload, List<string> errors) => new ServiceResult<T>(payload, true, errors);
    }

    /// <summary>
    /// JobResult
    /// </summary>
    public class ServiceResult
    {
        /// <summary>
        /// Returns if a result is successful and the errors if any exist
        /// </summary>
        /// <param name="successful"></param>
        /// <param name="errors"></param>
        protected ServiceResult(bool successful, List<string>? errors)
        {
            IsSuccessful = successful;
            if (errors != null && errors.Any())
                Errors.AddRange(errors);
        }

        /// <summary>
        /// Boolean parameter to determine if a result is successful
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// List of errors
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Aggregate errors
        /// </summary>
        public string ErrorsSummary => Errors.Any() ? Errors.Aggregate((x, y) => $"{x},{y}") : string.Empty;

        /// <summary>
        /// Returns the JobResult with a list of errors
        /// </summary>
        /// <param name="message"></param>
        /// <returns><see cref="ServiceResult"/></returns>
        public static ServiceResult WithError(string message) => new ServiceResult(false, new List<string> { message });

        /// <summary>
        /// Add error to the JobResult at the list of errors
        /// </summary>
        /// <param name="serviceresult"></param>
        /// <param name="message"></param>
        /// <returns><see cref="ServiceResult"/></returns>
        public static void AddError(ServiceResult serviceresult, string message) => serviceresult.Errors.Add(message);

        /// <summary>
        /// Returns the JobResult with a list of errors
        /// </summary>
        /// <param name="messages"></param>
        /// <returns><see cref="ServiceResult"/></returns>
        public static ServiceResult WithErrors(List<string> messages) => new ServiceResult(false, messages);

        /// <summary>
        /// Returns the successful JobResult
        /// </summary>
        /// <returns><see cref="ServiceResult"/></returns>
        public static ServiceResult Success => new ServiceResult(true, null);

        /// <summary>
        /// Returns the successful JobResult
        /// </summary>
        /// <returns><see cref="ServiceResult"/></returns>
        public static ServiceResult PartialSuccess(List<string> messages) => new ServiceResult(true, messages);

        /// <summary>
        /// Returns the successful JobResult
        /// </summary>
        /// <returns><see cref="ServiceResult"/></returns>
        public static ServiceResult Failed => new ServiceResult(false, new List<string>());
    }
}
