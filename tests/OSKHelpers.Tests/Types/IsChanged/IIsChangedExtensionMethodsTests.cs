using System;
using Moq;
using OSKHelpers.Types.IsChanged;

namespace OSKHelpers.Tests.Types.IsChanged
{
    /// <summary>
    /// Tests for the <see cref="IIsChangedExtensionMethods"/> class; <see cref="IIsChanged"/> is simulated with Moq.
    /// </summary>
    [TestClass]
    public class IIsChangedExtensionMethodsTests
    {
        #region Members

        private Mock<IIsChanged> _mock;

        #endregion

        #region Setup

        /// <summary>
        /// Creates the mock with IsChanged tracked as a normal property, initially false.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _mock = new Mock<IIsChanged>();
            _mock.SetupProperty(m => m.IsChanged, false);
        }

        #endregion

        #region SetProperty (main overload)

        /// <summary>
        /// Verifies that a different value updates the field, sets IsChanged and calls the callbacks.
        /// </summary>
        [TestMethod]
        public void SetProperty_DifferentValue_UpdatesFieldAndSetsIsChanged()
        {
            var field           = 1;
            int? changingValue  = null;
            (int Old, int New)? changed = null;

            var result = _mock.Object.SetProperty(ref field, 2, v => { changingValue = v; return true; }, (o, n) => changed = (o, n));

            Assert.IsTrue(result);
            Assert.AreEqual(2, field);
            Assert.IsTrue(_mock.Object.IsChanged);
            Assert.AreEqual(2, changingValue);
            Assert.AreEqual((1, 2), changed);
        }

        /// <summary>
        /// Verifies that the same value leaves everything unchanged and does not call the callbacks.
        /// </summary>
        [TestMethod]
        public void SetProperty_SameValue_ReturnsFalseWithoutCallbacks()
        {
            var field   = "A";
            var calls   = 0;

            var result = _mock.Object.SetProperty(ref field, "A", v => { calls++; return true; }, (o, n) => calls++);

            Assert.IsFalse(result);
            Assert.AreEqual(0, calls);
            Assert.IsFalse(_mock.Object.IsChanged);
        }

        /// <summary>
        /// Verifies that onValueChanging returning false cancels the change.
        /// </summary>
        [TestMethod]
        public void SetProperty_ChangingReturnsFalse_CancelsChange()
        {
            var field   = 1;
            var called  = false;

            var result = _mock.Object.SetProperty(ref field, 2, v => false, (o, n) => called = true);

            Assert.IsFalse(result);
            Assert.AreEqual(1, field);
            Assert.IsFalse(called);
            Assert.IsFalse(_mock.Object.IsChanged);
        }

        /// <summary>
        /// Verifies that the callbacks are optional.
        /// </summary>
        [TestMethod]
        public void SetProperty_NoCallbacks_UpdatesField()
        {
            string field = null;

            Assert.IsTrue(_mock.Object.SetProperty(ref field, "A"));
            Assert.AreEqual("A", field);
            Assert.IsTrue(_mock.Object.IsChanged);
        }

        #endregion

        #region SetProperty (callback overloads)

        /// <summary>
        /// Verifies the overloads with onValueChanging and an Action or Action&lt;T&gt; callback.
        /// </summary>
        [TestMethod]
        public void SetProperty_ChangingAndSimpleCallbacks_CallCallbacks()
        {
            var field       = 1;
            var actionCalls = 0;
            var newValue    = 0;

            Assert.IsTrue(_mock.Object.SetProperty(ref field, 2, v => true, () => actionCalls++));
            Assert.IsTrue(_mock.Object.SetProperty(ref field, 3, v => true, (int v) => newValue = v));
            Assert.IsFalse(_mock.Object.SetProperty(ref field, 4, v => false, () => actionCalls++));

            Assert.AreEqual(1, actionCalls);
            Assert.AreEqual(3, newValue);
            Assert.AreEqual(3, field);
        }

        /// <summary>
        /// Verifies the overloads with only the onValueChanged callback (Action&lt;T, T&gt;, Action, Action&lt;T&gt;).
        /// </summary>
        [TestMethod]
        public void SetProperty_OnlyChangedCallbacks_CallCallbacks()
        {
            var field       = 1;
            (int Old, int New)? changed = null;
            var actionCalls = 0;
            var newValue    = 0;

            Assert.IsTrue(_mock.Object.SetProperty(ref field, 2, (int o, int n) => changed = (o, n)));
            Assert.IsTrue(_mock.Object.SetProperty(ref field, 3, () => actionCalls++));
            Assert.IsTrue(_mock.Object.SetProperty(ref field, 4, (int v) => newValue = v));
            Assert.IsFalse(_mock.Object.SetProperty(ref field, 4, () => actionCalls++));

            Assert.AreEqual((1, 2), changed);
            Assert.AreEqual(1, actionCalls);
            Assert.AreEqual(4, newValue);
        }

        /// <summary>
        /// Verifies that a null Action or Action&lt;T&gt; callback is ignored, as in the main overload (test plan decision 4).
        /// </summary>
        [TestMethod]
        [TestCategory("Intervento-035")]
        public void SetProperty_NullSimpleCallbacks_IgnoresCallbacks()
        {
            var field = 1;

            Assert.IsTrue(_mock.Object.SetProperty(ref field, 2, (Action)null));
            Assert.IsTrue(_mock.Object.SetProperty(ref field, 3, (Action<int>)null));
            Assert.IsTrue(_mock.Object.SetProperty(ref field, 4, v => true, (Action)null));
            Assert.IsTrue(_mock.Object.SetProperty(ref field, 5, v => true, (Action<int>)null));

            Assert.AreEqual(5, field);
        }

        #endregion

        #region SetProperty (string conversions)

        /// <summary>
        /// Verifies the conversion of a string to decimal: IsChanged is set only when the value changes.
        /// </summary>
        [TestMethod]
        public void SetProperty_DecimalString_SetsIsChangedOnlyWhenChanged()
        {
            var field = 10m;

            Assert.IsFalse(_mock.Object.SetProperty(ref field, "10"));
            Assert.IsFalse(_mock.Object.IsChanged);
            Assert.IsTrue(_mock.Object.SetProperty(ref field, "12,5"));
            Assert.AreEqual(12.5m, field);
            Assert.IsTrue(_mock.Object.IsChanged);
        }

        /// <summary>
        /// Verifies the conversion of a string to nullable decimal, including the empty string.
        /// </summary>
        [TestMethod]
        public void SetProperty_NullableDecimalString_SetsIsChangedOnlyWhenChanged()
        {
            decimal? field = null;

            Assert.IsFalse(_mock.Object.SetProperty(ref field, ""));
            Assert.IsFalse(_mock.Object.IsChanged);
            Assert.IsTrue(_mock.Object.SetProperty(ref field, "12,5"));
            Assert.AreEqual(12.5m, field);
            Assert.IsTrue(_mock.Object.IsChanged);
        }

        #endregion

        #region SetIsChanged / ResetIsChanged

        /// <summary>
        /// Verifies that SetIsChanged sets IsChanged to the value passed (default true).
        /// </summary>
        [TestMethod]
        public void SetIsChanged_Value_SetsIsChanged()
        {
            _mock.Object.SetIsChanged();
            Assert.IsTrue(_mock.Object.IsChanged);

            _mock.Object.SetIsChanged(false);
            Assert.IsFalse(_mock.Object.IsChanged);
        }

        /// <summary>
        /// Verifies that ResetIsChanged calls ResetIsChangedExecute and sets IsChanged to false.
        /// </summary>
        [TestMethod]
        public void ResetIsChanged_Changed_CallsResetExecuteAndClearsIsChanged()
        {
            _mock.Object.IsChanged = true;

            _mock.Object.ResetIsChanged();

            Assert.IsFalse(_mock.Object.IsChanged);
            _mock.Verify(m => m.ResetIsChangedExecute(), Times.Once());
        }

        #endregion
    }
}
