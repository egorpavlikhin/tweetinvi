using System;
using System.Threading.Tasks;
using Tweetinvi.Client.Requesters;
using Tweetinvi.Core.RateLimit;
using Tweetinvi.Models;
using Tweetinvi.Parameters.HelpClient;

namespace Tweetinvi.Client
{
    public class RateLimitsClient : IRateLimitsClient
    {
        private readonly TwitterClient _client;
        private readonly IRateLimitCacheManager _rateLimitCacheManager;
        private readonly IRateLimitHelper _rateLimitHelper;
        private readonly IHelpRequester _helpRequester;

        public RateLimitsClient(TwitterClient client, IRateLimitCacheManager rateLimitCacheManager)
        {
            var executionContext = client.CreateTwitterExecutionContext();

            _client = client;
            _helpRequester = client.RequestExecutor.Help;
            _rateLimitCacheManager = rateLimitCacheManager;
            _rateLimitHelper = executionContext.Container.Resolve<IRateLimitHelper>();
        }

        public async Task InitializeRateLimitsManager()
        {
            if (await _rateLimitCacheManager.RateLimitCache.GetCredentialsRateLimits(_client.Credentials) == null)
            {
                await _rateLimitCacheManager.RefreshCredentialsRateLimits(_client.Credentials).ConfigureAwait(false);
            }
        }

        public Task<ICredentialsRateLimits> GetRateLimits()
        {
            return GetRateLimits(new GetRateLimitsParameters());
        }

        public Task<ICredentialsRateLimits> GetRateLimits(RateLimitsSource from)
        {
            return GetRateLimits(new GetRateLimitsParameters
            {
                From = from
            });
        }

        public async Task<ICredentialsRateLimits> GetRateLimits(IGetRateLimitsParameters parameters)
        {
            switch (parameters.From)
            {
                case RateLimitsSource.CacheOnly:
                    return await _rateLimitCacheManager.RateLimitCache.GetCredentialsRateLimits(_client.Credentials).ConfigureAwait(false);
                case RateLimitsSource.TwitterApiOnly:
                    var twitterResult = await _helpRequester.GetRateLimits(parameters).ConfigureAwait(false);
                    return twitterResult?.DataTransferObject;
                case RateLimitsSource.CacheOrTwitterApi:
                    return await _rateLimitCacheManager.GetCredentialsRateLimits(_client.Credentials).ConfigureAwait(false);
                default:
                    throw new ArgumentException(nameof(parameters.From));
            }
        }

        public Task<IEndpointRateLimit> GetEndpointRateLimit(string url)
        {
            return GetEndpointRateLimit(new GetEndpointRateLimitsParameters(url));
        }

        public Task<IEndpointRateLimit> GetEndpointRateLimit(string url, RateLimitsSource from)
        {
            return GetEndpointRateLimit(new GetEndpointRateLimitsParameters(url)
            {
                From = from
            });
        }

        public Task<IEndpointRateLimit> GetEndpointRateLimit(IGetEndpointRateLimitsParameters parameters)
        {
            return _rateLimitCacheManager.GetQueryRateLimit(parameters, _client.Credentials);
        }
    }
}