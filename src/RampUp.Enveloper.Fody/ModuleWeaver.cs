using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace RampUp.Enveloper.Fody
{
    public class ModuleWeaver
    {
        public ModuleDefinition ModuleDefinition { get; set; }

        public IAssemblyResolver AssemblyResolver { get; set; }

        public Action<string> LogInfo { get; set; }
        public Action<string> LogWarning { get; set; }

        public void Execute()
        {
            var isRampUp = ModuleDefinition.Assembly.Name.Name == "RampUp";

            var rampUp = isRampUp ? ModuleDefinition.Assembly : AssemblyResolver.Resolve("RampUp");
            var envelopeType = rampUp.MainModule.Types.Single(t => t.FullName == "RampUp.Actors.Envelope");
            var messageMarkupInterface = rampUp.MainModule.Types.Single(t => t.Name == "IMessage");
            var markupFullName = messageMarkupInterface.FullName;
            var messageTypes = ModuleDefinition.Types.SelectMany(AllTypes).Where(t => t.IsValueType && t.Interfaces.Any() && t.Interfaces.Any(i => i.FullName == markupFullName)).ToArray();

            if (messageTypes.Any() == false)
            {
                LogWarning(
                    "No RampUp messages were found. Ensure that your messages implement an interface with a full name 'RampUp.Actors.IMessage'");
                return;
            }

            var envelopeImported = isRampUp? envelopeType : ModuleDefinition.Import(envelopeType);

            foreach (var messageType in messageTypes)
            {
                messageType.Fields.Add(new FieldDefinition("_____envelope", FieldAttributes.Private, envelopeImported));
            }
        }

        private static IEnumerable<TypeDefinition> AllTypes(TypeDefinition t)
        {
            yield return t;

            foreach (var nested in t.NestedTypes.SelectMany(AllTypes))
            {
                yield return nested;
            }
        }
    }
}
