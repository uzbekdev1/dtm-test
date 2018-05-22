/* 
 * Optical Mark Recognition 
 * Copyright 2015, Justin Fyfe
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * Author: Justin
 * Date: 4-18-2015
 */

using OmrMarkEngine.Template;

namespace OmrMarkEngine.Output.Transforms
{
    /// <summary>
    ///     Output transform interface
    /// </summary>
    public interface IOutputTransform
    {
        /// <summary>
        ///     Name of the output transform
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Extension of the output transform
        /// </summary>
        string Extension { get; }

        /// <summary>
        ///     Perform the transform
        /// </summary>
        byte[] Transform(OmrTemplate template, OmrPageOutputCollection pages);
    }
}