using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class ItemInteractionPortabilityTests
    {
        #region Public Methods

        [Test]
        public void ActiveSource_CanStartUsingAnItemCreatedAtRuntime()
        {
            var item = new RuntimeItem();
            var source = Substitute.For<IActiveItemSource>();
            source.ActiveItem.Returns(item);
            var controller = Substitute.For<IInteractionController>();
            controller.Supports(item, null).Returns(true);
            controller.TryBeginInteraction(item, null, Arg.Any<Ray>(), Arg.Any<Vector3>(), Arg.Any<Vector2>()).Returns(true);
            controller.IsInteractionActive.Returns(true);
            var input = new InteractionInputState(new InteractionControllerResolver(new[] { controller }));

            bool hasStarted = input.PressItem(source.ActiveItem, null, default, default, default);

            Assert.That(hasStarted, Is.True);
            Assert.That(input.HasActiveInteraction, Is.True);
            controller.Received(1).TryBeginInteraction(item, null, Arg.Any<Ray>(), Arg.Any<Vector3>(), Arg.Any<Vector2>());
        }

        [Test]
        public void RuntimeAssembly_DoesNotDependOnMinecraftOrCharacterController()
        {
            foreach (System.Reflection.AssemblyName reference in typeof(InteractionInputState).Assembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name, Does.Not.StartWith("Minecraft."));
                Assert.That(reference.Name, Does.Not.StartWith("PhysicsCharacterController"));
            }
        }

        #endregion

        private sealed class RuntimeItem : IInteractionItem
        {
            public string DisplayName => "Runtime equipment";
        }
    }
}
