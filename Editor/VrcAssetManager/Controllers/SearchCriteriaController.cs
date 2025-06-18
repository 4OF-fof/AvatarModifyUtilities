using System;
using AMU.Editor.VrcAssetManager.Schema;

namespace AMU.Editor.VrcAssetManager.Controllers
{
    /// <summary>
    /// 検索条件の生成と管理
    /// </summary>
    public static class SearchCriteriaController
    {
        /// <summary>
        /// 日付範囲の生成
        /// </summary>
        public static class DateRangeFactory
        {
            /// <summary>
            /// 過去N日間の範囲を生成
            /// </summary>
            public static DateRange LastDays(int days)
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddDays(-days);
                return new DateRange(startDate, endDate, true);
            }

            /// <summary>
            /// 過去N月間の範囲を生成
            /// </summary>
            public static DateRange LastMonths(int months)
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-months);
                return new DateRange(startDate, endDate, true);
            }

            /// <summary>
            /// 過去N年間の範囲を生成
            /// </summary>
            public static DateRange LastYears(int years)
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddYears(-years);
                return new DateRange(startDate, endDate, true);
            }

            /// <summary>
            /// 今日の範囲を生成
            /// </summary>
            public static DateRange Today()
            {
                var today = DateTime.Today;
                return new DateRange(today, today.AddDays(1).AddTicks(-1), true);
            }

            /// <summary>
            /// 今週の範囲を生成
            /// </summary>
            public static DateRange ThisWeek()
            {
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);
                return new DateRange(startOfWeek, endOfWeek, true);
            }

            /// <summary>
            /// 今月の範囲を生成
            /// </summary>
            public static DateRange ThisMonth()
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
                return new DateRange(startOfMonth, endOfMonth, true);
            }

            /// <summary>
            /// 今年の範囲を生成
            /// </summary>
            public static DateRange ThisYear()
            {
                var today = DateTime.Today;
                var startOfYear = new DateTime(today.Year, 1, 1);
                var endOfYear = startOfYear.AddYears(1).AddTicks(-1);
                return new DateRange(startOfYear, endOfYear, true);
            }

            /// <summary>
            /// 無効化された範囲を生成
            /// </summary>
            public static DateRange Disabled => new DateRange(DateTime.MinValue, DateTime.MaxValue, false);
        }

        /// <summary>
        /// ファイルサイズ範囲の生成
        /// </summary>
        public static class FileSizeRangeFactory
        {
            /// <summary>
            /// 小さなファイル（1MB未満）
            /// </summary>
            public static FileSizeRange Small()
            {
                return new FileSizeRange(
                    new FileSize(0),
                    new FileSize(1024 * 1024),
                    true);
            }

            /// <summary>
            /// 中程度のファイル（1MB-10MB）
            /// </summary>
            public static FileSizeRange Medium()
            {
                return new FileSizeRange(
                    new FileSize(1024 * 1024),
                    new FileSize(10 * 1024 * 1024),
                    true);
            }

            /// <summary>
            /// 大きなファイル（10MB-100MB）
            /// </summary>
            public static FileSizeRange Large()
            {
                return new FileSizeRange(
                    new FileSize(10 * 1024 * 1024),
                    new FileSize(100 * 1024 * 1024),
                    true);
            }

            /// <summary>
            /// 非常に大きなファイル（100MB以上）
            /// </summary>
            public static FileSizeRange VeryLarge()
            {
                return new FileSizeRange(
                    new FileSize(100 * 1024 * 1024),
                    new FileSize(long.MaxValue),
                    true);
            }

            /// <summary>
            /// カスタム範囲を生成
            /// </summary>
            public static FileSizeRange Custom(long minBytes, long maxBytes)
            {
                return new FileSizeRange(
                    new FileSize(minBytes),
                    new FileSize(maxBytes),
                    true);
            }

            /// <summary>
            /// KB単位でカスタム範囲を生成
            /// </summary>
            public static FileSizeRange CustomKB(long minKB, long maxKB)
            {
                return Custom(minKB * 1024, maxKB * 1024);
            }

            /// <summary>
            /// MB単位でカスタム範囲を生成
            /// </summary>
            public static FileSizeRange CustomMB(long minMB, long maxMB)
            {
                return Custom(minMB * 1024 * 1024, maxMB * 1024 * 1024);
            }

            /// <summary>
            /// 指定サイズ以下の範囲を生成
            /// </summary>
            public static FileSizeRange UpTo(FileSize maxSize)
            {
                return new FileSizeRange(new FileSize(0), maxSize, true);
            }

            /// <summary>
            /// 指定サイズ以上の範囲を生成
            /// </summary>
            public static FileSizeRange AtLeast(FileSize minSize)
            {
                return new FileSizeRange(minSize, new FileSize(long.MaxValue), true);
            }

            /// <summary>
            /// 無効化された範囲を生成
            /// </summary>
            public static FileSizeRange Disabled => new FileSizeRange(new FileSize(0), new FileSize(long.MaxValue), false);
        }

        /// <summary>
        /// よく使用される検索条件の組み合わせ
        /// </summary>
        public static class CommonCriteria
        {
            /// <summary>
            /// 最近追加されたアセット（過去7日）
            /// </summary>
            public static (DateRange dateRange, FileSizeRange sizeRange) RecentlyAdded()
            {
                return (DateRangeFactory.LastDays(7), FileSizeRangeFactory.Disabled);
            }

            /// <summary>
            /// 今月のアセット
            /// </summary>
            public static (DateRange dateRange, FileSizeRange sizeRange) ThisMonth()
            {
                return (DateRangeFactory.ThisMonth(), FileSizeRangeFactory.Disabled);
            }

            /// <summary>
            /// 大きなファイルのみ
            /// </summary>
            public static (DateRange dateRange, FileSizeRange sizeRange) LargeFilesOnly()
            {
                return (DateRangeFactory.Disabled, FileSizeRangeFactory.Large());
            }

            /// <summary>
            /// 最近の大きなファイル
            /// </summary>
            public static (DateRange dateRange, FileSizeRange sizeRange) RecentLargeFiles()
            {
                return (DateRangeFactory.LastDays(30), FileSizeRangeFactory.Large());
            }
        }
    }
}
