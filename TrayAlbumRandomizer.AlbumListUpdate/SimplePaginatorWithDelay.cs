namespace TrayAlbumRandomizer.AlbumListUpdate
{
    using SpotifyAPI.Web;
    using SpotifyAPI.Web.Http;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class SimplePaginatorWithDelay : IPaginator
    {
        private readonly int _delay = 0;

        public SimplePaginatorWithDelay(int delay)
        {
            _delay = delay;
        }

        protected virtual Task<bool> ShouldContinue<T>(List<T> results, IPaginatable<T> page)
        {
            return Task.FromResult(true);
        }

        protected virtual Task<bool> ShouldContinue<T, TNext>(List<T> results, IPaginatable<T, TNext> page)
        {
            return Task.FromResult(true);
        }

        public async Task<IList<T>> PaginateAll<T>(IPaginatable<T> firstPage, IAPIConnector connector)
        {
            var page = firstPage;
            var results = new List<T>();
            results.AddRange(firstPage.Items);

            while (page.Next != null && await ShouldContinue(results, page).ConfigureAwait(false))
            {
                page = await connector.Get<Paging<T>>(new Uri(page.Next, UriKind.Absolute)).ConfigureAwait(false);
                results.AddRange(page.Items);
                Thread.Sleep(_delay);
            }

            return results;
        }

        public async Task<IList<T>> PaginateAll<T, TNext>(
          IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper, IAPIConnector connector
        )
        {
            var page = firstPage;
            var results = new List<T>();
            results.AddRange(firstPage.Items);

            while (page.Next != null && await ShouldContinue(results, page).ConfigureAwait(false))
            {
                var next = await connector.Get<TNext>(new Uri(page.Next, UriKind.Absolute)).ConfigureAwait(false);
                page = mapper(next);
                results.AddRange(page.Items);
                Thread.Sleep(_delay);
            }

            return results;
        }
    }
}
