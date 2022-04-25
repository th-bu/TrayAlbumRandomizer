namespace TrayAlbumRandomizer.AlbumListUpdate
{
    using SpotifyAPI.Web;
    using SpotifyAPI.Web.Http;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class SimplePaginatorWithDelay : IPaginator
    {
        private readonly int delay;

        public SimplePaginatorWithDelay(int delay)
        {
            this.delay = delay;
        }

        private static Task<bool> ShouldContinue<T>(List<T> results, IPaginatable<T> page)
        {
            return Task.FromResult(true);
        }

        private static Task<bool> ShouldContinue<T, TNext>(List<T> results, IPaginatable<T, TNext> page)
        {
            return Task.FromResult(true);
        }

        public async Task<IList<T>> PaginateAll<T>(IPaginatable<T> firstPage, IAPIConnector connector)
        {
            IPaginatable<T> page = firstPage;
            var results = new List<T>();

            if (firstPage.Items == null)
            {
                return results;
            }

            results.AddRange(firstPage.Items);

            while (page.Next != null && await ShouldContinue(results, page).ConfigureAwait(false))
            {
                page = await connector.Get<Paging<T>>(new Uri(page.Next, UriKind.Absolute)).ConfigureAwait(false);
                    
                if (page.Items != null)
                {
                    results.AddRange(page.Items);
                }

                Thread.Sleep(this.delay);
            }

            return results;
        }

        public async Task<IList<T>> PaginateAll<T, TNext>(IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper,
            IAPIConnector connector)
        {
            IPaginatable<T, TNext> page = firstPage;
            var results = new List<T>();
            
            if (firstPage.Items == null)
            {
                return results;
            }

            results.AddRange(firstPage.Items);

            while (page.Next != null && await ShouldContinue(results, page).ConfigureAwait(false))
            {
                TNext next = await connector.Get<TNext>(new Uri(page.Next, UriKind.Absolute)).ConfigureAwait(false);
                page = mapper(next);
                
                if (page.Items != null)
                {
                    results.AddRange(page.Items);
                }

                Thread.Sleep(this.delay);
            }

            return results;
        }
    }
}
