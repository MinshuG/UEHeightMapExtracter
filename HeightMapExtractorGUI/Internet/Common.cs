﻿using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HeightMapExtractorGUI.Internet;

// https://stackoverflow.com/questions/19260060/retrying-httpclient-unsuccessful-requests
public class RetryHandler : DelegatingHandler
{
    // Strongly consider limiting the number of retries - "retry forever" is
    // probably not the most user-friendly way you could respond to "the
    // network cable got pulled out."
    private const int MaxRetries = 3;

    public RetryHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    { }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = null;
        for (int i = 0; i < MaxRetries; i++)
        {
            response = await base.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode) {
                return response;
            }
        }

        return response;
    }
}