using System;

namespace RampUp.Actors.Impl
{
    public interface IStructSizeCounter
    {
        /// <summary>
        /// Calculates the size of the managed representation of a struc.
        /// </summary>
        /// <param name="struct"></param>
        /// <returns>The managed size.</returns>
        int GetSize(Type @struct);
    }
}