using System.Collections.Generic;
using NUnit.Framework;

namespace FakeMG.Settings.Tests
{
    public sealed class SettingsNavigationGraphTests
    {
        private static readonly string[] TABS = { "Display Tab", "Audio Tab", "Controls Tab" };

        [Test]
        public void GroupIntoRows_SelectablesOfOneRow_ShareARow()
        {
            var rowOfSelectable = new Dictionary<string, string> { ["Primary"] = "Jump Row", ["Secondary"] = "Jump Row" };

            List<IReadOnlyList<string>> rows = SettingsNavigationGraph<string>.GroupIntoRows(new[] { "Primary", "Secondary" }, rowOfSelectable);

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0], Is.EqualTo(new[] { "Primary", "Secondary" }));
        }

        [Test]
        public void GroupIntoRows_SelectablesWithoutARow_EachGetARow()
        {
            List<IReadOnlyList<string>> rows = SettingsNavigationGraph<string>.GroupIntoRows(
                new[] { "Reset", "Back" },
                new Dictionary<string, string>());

            Assert.That(rows, Has.Count.EqualTo(2));
        }

        [Test]
        public void GroupIntoRows_EmptyPage_ReturnsNoRows()
        {
            List<IReadOnlyList<string>> rows = SettingsNavigationGraph<string>.GroupIntoRows(new string[0], new Dictionary<string, string>());

            Assert.That(rows, Is.Empty);
        }

        [Test]
        public void GetSelectableLinks_FirstRow_LeadsUpToTheShownTab()
        {
            var graph = new SettingsNavigationGraph<string>(Rows(new[] { "Volume" }, new[] { "Music" }), TABS, shownTabIndex: 1);

            NavigationLinks<string> links = graph.GetSelectableLinks(0, 0);

            Assert.That(links.Up, Is.EqualTo("Audio Tab"));
            Assert.That(links.Down, Is.EqualTo("Music"));
        }

        [Test]
        public void GetSelectableLinks_LastRow_LeadsNowhereDown()
        {
            var graph = new SettingsNavigationGraph<string>(Rows(new[] { "Volume" }, new[] { "Music" }), TABS, 1);

            Assert.That(graph.GetSelectableLinks(1, 0).Down, Is.Null);
        }

        [Test]
        public void GetSelectableLinks_RowEnds_LeaveLeftAndRightEmpty()
        {
            var graph = new SettingsNavigationGraph<string>(Rows(new[] { "Primary", "Secondary" }), TABS, 2);

            Assert.That(graph.GetSelectableLinks(0, 0).Left, Is.Null);
            Assert.That(graph.GetSelectableLinks(0, 0).Right, Is.EqualTo("Secondary"));
            Assert.That(graph.GetSelectableLinks(0, 1).Right, Is.Null);
        }

        [Test]
        public void GetSelectableLinks_ShorterRowBelow_LeadsToItsLastSelectable()
        {
            var graph = new SettingsNavigationGraph<string>(Rows(new[] { "Primary", "Secondary" }, new[] { "Reset" }), TABS, 2);

            Assert.That(graph.GetSelectableLinks(0, 1).Down, Is.EqualTo("Reset"));
        }

        [Test]
        public void GetTabLinks_LeadDownToTheFirstSelectableAndAcrossTabs()
        {
            var graph = new SettingsNavigationGraph<string>(Rows(new[] { "Window Mode" }), TABS, 0);

            NavigationLinks<string> links = graph.GetTabLinks(1);

            Assert.That(links.Down, Is.EqualTo("Window Mode"));
            Assert.That(links.Left, Is.EqualTo("Display Tab"));
            Assert.That(links.Right, Is.EqualTo("Controls Tab"));
            Assert.That(links.Up, Is.Null);
        }

        [Test]
        public void GetTabLinks_PageWithoutSelectables_LeadsNowhereDown()
        {
            var graph = new SettingsNavigationGraph<string>(Rows(), TABS, 0);

            Assert.That(graph.GetTabLinks(0).Down, Is.Null);
        }

        private static IReadOnlyList<IReadOnlyList<string>> Rows(params string[][] rows)
        {
            return rows;
        }
    }
}
