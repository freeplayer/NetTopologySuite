using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;

using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.Utilities;

namespace GisSharpBlog.NetTopologySuite.IO
{
    /// <summary> 
    /// Outputs the textual representation of a <see cref="Geometry" />.
    /// The <see cref="WKTWriter" /> outputs coordinates rounded to the precision
    /// model. No more than the maximum number of necessary decimal places will be
    /// output.
    /// The Well-known Text format is defined in the <A
    /// HREF="http://www.opengis.org/techno/specs.htm">OpenGIS Simple Features
    /// Specification for SQL</A>.
    /// A non-standard "LINEARRING" tag is used for LinearRings. The WKT spec does
    /// not define a special tag for LinearRings. The standard tag to use is
    /// "LINESTRING".
    /// </summary>
    public class WKTWriter 
    {
        /// <summary>
        /// Generates the WKT for a <c>Point</c>.
        /// </summary>
        /// <param name="p0">The point coordinate.</param>
        /// <returns></returns>
        public static String ToPoint(Coordinate p0)
        {
            return "POINT ( " + p0.X + " " + p0.Y + " )";
        }

        /// <summary>
        /// Generates the WKT for a N-point <c>LineString</c>.
        /// </summary>
        /// <param name="seq">The sequence to output.</param>
        /// <returns></returns>
        public static String ToLineString(ICoordinateSequence seq)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("LINESTRING ");
            if (seq.Count == 0)
                buf.Append(" EMPTY");
            else 
            {
                buf.Append("(");
                for (int i = 0; i < seq.Count; i++) 
                {
                    if (i > 0)
                        buf.Append(", ");
                    buf.Append(seq.GetX(i) + " " + seq.GetY(i));
              }
              buf.Append(")");
            }
            return buf.ToString();
        }

        /// <summary>
        /// Generates the WKT for a 2-point <c>LineString</c>.
        /// </summary>
        /// <param name="p0">The first coordinate.</param>
        /// <param name="p1">The second coordinate.</param>
        /// <returns></returns>
        public static String ToLineString(Coordinate p0, Coordinate p1)
        {
            return "LINESTRING ( " + p0.X + " " + p0.Y + ", " + p1.X + " " + p1.Y + " )";
        }

        // NOTE: modified for "safe" assembly in Sql 2005
        // const added!
        private const int WKTWriterIndent = 2;

        /// <summary>  
        /// Creates the <c>NumberFormatInfo</c> used to write <c>double</c>s
        /// with a sufficient number of decimal places.
        /// </summary>
        /// <param name="precisionModel"> 
        /// The <c>PrecisionModel</c> used to determine
        /// the number of decimal places to write.
        /// </param>
        /// <returns>
        /// A <c>NumberFormatInfo</c> that write <c>double</c>
        /// s without scientific notation.
        /// </returns>
        private static NumberFormatInfo CreateFormatter(PrecisionModel precisionModel) 
        {
            // the default number of decimal places is 16, which is sufficient
            // to accomodate the maximum precision of a double.
            int decimalPlaces = precisionModel.MaximumSignificantDigits;
            // specify decimal separator explicitly to avoid problems in other locales
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberDecimalDigits = decimalPlaces;
            nfi.NumberGroupSeparator = String.Empty;
            nfi.NumberGroupSizes = new int[] { };
            return nfi;            
        }

        /// <summary>
        /// Returns a <c>String</c> of repeated characters.
        /// </summary>
        /// <param name="ch">The character to repeat.</param>
        /// <param name="count">The number of times to repeat the character.</param>
        /// <returns>A <c>string</c> of characters.</returns>
        public static string StringOfChar(char ch, int count) 
        {
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < count; i++) 
                buf.Append(ch);            
            return buf.ToString();
        }

        private NumberFormatInfo formatter;
        private bool isFormatted = false;        

        /// <summary>
        /// 
        /// </summary>
        public WKTWriter() { }

        /// <summary>
        /// Converts a <c>Geometry</c> to its Well-known Text representation.
        /// </summary>
        /// <param name="geometry">A <c>Geometry</c> to process.</param>
        /// <returns>A Geometry Tagged Text string (see the OpenGIS Simple Features Specification).</returns>
        public virtual string Write(Geometry geometry)
        {
            TextWriter sw = new StringWriter();
            try 
            {
                WriteFormatted(geometry, false, sw);
            }
            catch (IOException)
            {
                Assert.ShouldNeverReachHere();
            }
            return sw.ToString();
        }

        /// <summary>
        /// Converts a <c>Geometry</c> to its Well-known Text representation.
        /// </summary>
        /// <param name="geometry">A <c>Geometry</c> to process.</param>
        /// <param name="writer"></param>
        /// <returns>A "Geometry Tagged Text" string (see the OpenGIS Simple Features Specification)</returns>
        public virtual void Write(Geometry geometry, TextWriter writer)
        {
            WriteFormatted(geometry, false, writer);
        }

        /// <summary>
        /// Same as <c>write</c>, but with newlines and spaces to make the
        /// well-known text more readable.
        /// </summary>
        /// <param name="geometry">A <c>Geometry</c> to process</param>
        /// <returns>
        /// A "Geometry Tagged Text" string (see the OpenGIS Simple
        /// Features Specification), with newlines and spaces.
        /// </returns>
        public virtual string WriteFormatted(Geometry geometry)
        {
            TextWriter sw = new StringWriter();
            try 
            {
                WriteFormatted(geometry, true, sw);
            }
            catch (IOException) 
            {
                Assert.ShouldNeverReachHere();
            }
            return sw.ToString();
        }

        /// <summary>
        /// Same as <c>write</c>, but with newlines and spaces to make the
        /// well-known text more readable.
        /// </summary>
        /// <param name="geometry">A <c>Geometry</c> to process</param>
        /// <param name="writer"></param>
        /// <returns>
        /// A Geometry Tagged Text string (see the OpenGIS Simple
        /// Features Specification), with newlines and spaces.
        /// </returns>
        public virtual void WriteFormatted(Geometry geometry, TextWriter writer)
        {
            WriteFormatted(geometry, true, writer);
        }

        /// <summary>
        /// Converts a <c>Geometry</c> to its Well-known Text representation.
        /// </summary>
        /// <param name="geometry">A <c>Geometry</c> to process</param>
        /// <param name="isFormatted"></param>
        /// <param name="writer"></param>
        /// <returns>
        /// A "Geometry Tagged Text" string (see the OpenGIS Simple
        /// Features Specification).
        /// </returns>
        private void WriteFormatted(Geometry geometry, bool isFormatted, TextWriter writer)
        {
            this.isFormatted = isFormatted;
            formatter = CreateFormatter(geometry.PrecisionModel);
            AppendGeometryTaggedText(geometry, 0, writer);
        }

        /// <summary>
        /// Converts a <c>Geometry</c> to &lt;Geometry Tagged Text format,
        /// then appends it to the writer.
        /// </summary>
        /// <param name="geometry">/he <c>Geometry</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">/he output writer to append to.</param>
        private void AppendGeometryTaggedText(Geometry geometry, int level, TextWriter writer)
        {
            Indent(level, writer);

            if (geometry is Point) 
            {
                Point point = (Point) geometry;
                AppendPointTaggedText((Coordinate) point.Coordinate, level, writer, point.PrecisionModel);
            }
            else if (geometry is LinearRing) 
                AppendLinearRingTaggedText((LinearRing) geometry, level, writer);            
            else if (geometry is LineString) 
                AppendLineStringTaggedText((LineString) geometry, level, writer);            
            else if (geometry is Polygon) 
                AppendPolygonTaggedText((Polygon) geometry, level, writer);
            else if (geometry is MultiPoint) 
                AppendMultiPointTaggedText((MultiPoint) geometry, level, writer);            
            else if (geometry is MultiLineString) 
                AppendMultiLineStringTaggedText((MultiLineString) geometry, level, writer);            
            else if (geometry is MultiPolygon) 
                AppendMultiPolygonTaggedText((MultiPolygon) geometry, level, writer);            
            else if (geometry is GeometryCollection) 
                AppendGeometryCollectionTaggedText((GeometryCollection) geometry, level, writer);
            else Assert.ShouldNeverReachHere("Unsupported Geometry implementation:" + geometry.GetType());
        }

        /// <summary>
        /// Converts a <c>Coordinate</c> to Point Tagged Text format,
        /// then appends it to the writer.
        /// </summary>
        /// <param name="coordinate">The <c>Coordinate</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        /// <param name="precisionModel"> 
        /// The <c>PrecisionModel</c> to use to convert
        /// from a precise coordinate to an external coordinate.
        /// </param>
        private void AppendPointTaggedText(Coordinate coordinate, int level, TextWriter writer, 
                                           PrecisionModel precisionModel)
        {
            writer.Write("POINT ");
            AppendPointText(coordinate, level, writer, precisionModel);
        }

        /// <summary>
        /// Converts a <c>LineString</c> to &lt;LineString Tagged Text
        /// format, then appends it to the writer.
        /// </summary>
        /// <param name="lineString">The <c>LineString</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendLineStringTaggedText(LineString lineString, int level, TextWriter writer)
        {
            writer.Write("LINESTRING ");
            AppendLineStringText(lineString, level, false, writer);
        }

        /// <summary>
        /// Converts a <c>LinearRing</c> to &lt;LinearRing Tagged Text
        /// format, then appends it to the writer.
        /// </summary>
        /// <param name="linearRing">The <c>LinearRing</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendLinearRingTaggedText(LinearRing linearRing, int level, TextWriter writer)
        {
            writer.Write("LINEARRING ");
            AppendLineStringText(linearRing, level, false, writer);
        }

        /// <summary>
        /// Converts a <c>Polygon</c> to Polygon Tagged Text format,
        /// then appends it to the writer.
        /// </summary>
        /// <param name="polygon">The <c>Polygon</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendPolygonTaggedText(Polygon polygon, int level, TextWriter writer)
        {
            writer.Write("POLYGON ");
            AppendPolygonText(polygon, level, false, writer);
        }

        /// <summary>
        /// Converts a <c>MultiPoint</c> to &lt;MultiPoint Tagged Text
        /// format, then appends it to the writer.
        /// </summary>
        /// <param name="multipoint">The <c>MultiPoint</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendMultiPointTaggedText(MultiPoint multipoint, int level, TextWriter writer)
        {
            writer.Write("MULTIPOINT ");
            AppendMultiPointText(multipoint, level, writer);
        }

        /// <summary>
        /// Converts a <c>MultiLineString</c> to MultiLineString Tagged
        /// Text format, then appends it to the writer.
        /// </summary>
        /// <param name="multiLineString">The <c>MultiLineString</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendMultiLineStringTaggedText(MultiLineString multiLineString, int level, TextWriter writer)
        {
            writer.Write("MULTILINESTRING ");
            AppendMultiLineStringText(multiLineString, level, false, writer);
        }

        /// <summary>
        /// Converts a <c>MultiPolygon</c> to MultiPolygon Tagged Text
        /// format, then appends it to the writer.
        /// </summary>
        /// <param name="multiPolygon">The <c>MultiPolygon</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendMultiPolygonTaggedText(MultiPolygon multiPolygon, int level, TextWriter writer)
        {
            writer.Write("MULTIPOLYGON ");
            AppendMultiPolygonText(multiPolygon, level, writer);
        }

        /// <summary>
        /// Converts a <c>GeometryCollection</c> to GeometryCollection
        /// Tagged Text format, then appends it to the writer.
        /// </summary>
        /// <param name="geometryCollection">The <c>GeometryCollection</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendGeometryCollectionTaggedText(GeometryCollection geometryCollection, int level,
                                                        TextWriter writer)
        {
            writer.Write("GEOMETRYCOLLECTION ");
            AppendGeometryCollectionText(geometryCollection, level, writer);
        }

        /// <summary>
        /// Converts a <c>Coordinate</c> to Point Text format, then
        /// appends it to the writer.
        /// </summary>
        /// <param name="coordinate">The <c>Coordinate</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        /// <param name="precisionModel">
        /// The <c>PrecisionModel</c> to use to convert
        /// from a precise coordinate to an external coordinate.
        /// </param>
        private void AppendPointText(Coordinate coordinate, int level, TextWriter writer, PrecisionModel precisionModel)
        {
            if (coordinate == null) 
                writer.Write("EMPTY");            
            else 
            {
                writer.Write("(");
                AppendCoordinate(coordinate, writer, precisionModel);
                writer.Write(")");
            }
        }

        /// <summary>
        /// Converts a <c>Coordinate</c> to Point format, then appends
        /// it to the writer.
        /// </summary>
        /// <param name="coordinate">The <c>Coordinate</c> to process.</param>
        /// <param name="writer">The output writer to append to.</param>
        /// <param name="precisionModel">
        /// The <c>PrecisionModel</c> to use to convert
        /// from a precise coordinate to an external coordinate.
        /// </param>
        private void AppendCoordinate(Coordinate coordinate, TextWriter writer, PrecisionModel precisionModel)
        {	              
            writer.Write(WriteNumber(coordinate.X) + " " + WriteNumber(coordinate.Y));
        }

        /// <summary>
        /// Converts a <see cref="double" /> to a <see cref="string" />, 
        /// not in scientific notation.
        /// </summary>
        /// <param name="d">The <see cref="double" /> to convert.</param>
        /// <returns>
        /// The <see cref="double" /> as a <see cref="string" />, 
        /// not in scientific notation.
        /// </returns>
        private string WriteNumber(double d) 
        {           
            // return Convert.ToString(d, formatter) not generate decimals well formatted!
		    return d.ToString("N", formatter);	   
        }

        /// <summary>
        /// Converts a <c>LineString</c> to &lt;LineString Text format, then
        /// appends it to the writer.
        /// </summary>
        /// <param name="lineString">The <c>LineString</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="doIndent"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendLineStringText(LineString lineString, int level, bool doIndent, TextWriter writer)
        {
            if (lineString.IsEmpty)
                writer.Write("EMPTY");            
            else 
            {
                if (doIndent) Indent(level, writer);
                writer.Write("(");
                for (int i = 0; i < lineString.NumPoints; i++) 
                {
                    if (i > 0) 
                    {
                        writer.Write(", ");
                        if (i % 10 == 0) Indent(level + 2, writer);
                    }
                    AppendCoordinate(lineString.GetCoordinateN(i), writer, lineString.PrecisionModel);
                }
                writer.Write(")");
            }
        }

        /// <summary>
        /// Converts a <c>Polygon</c> to Polygon Text format, then
        /// appends it to the writer.
        /// </summary>
        /// <param name="polygon">The <c>Polygon</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="indentFirst"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendPolygonText(Polygon polygon, int level, bool indentFirst, TextWriter writer)
        {
            if (polygon.IsEmpty) 
                writer.Write("EMPTY");            
            else 
            {
                if (indentFirst) Indent(level, writer);
                writer.Write("(");
                AppendLineStringText((LineString) polygon.ExteriorRing, level, false, writer);
                for (int i = 0; i < polygon.NumInteriorRings; i++) 
                {
                    writer.Write(", ");
                    AppendLineStringText((LineString) polygon.GetInteriorRingN(i), level + 1, true, writer);
                }
                writer.Write(")");
            }
        }

        /// <summary>
        /// Converts a <c>MultiPoint</c> to &lt;MultiPoint Text format, then
        /// appends it to the writer.
        /// </summary>
        /// <param name="multiPoint">The <c>MultiPoint</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendMultiPointText(MultiPoint multiPoint, int level, TextWriter writer)
        {
            if (multiPoint.IsEmpty) 
                writer.Write("EMPTY");            
            else 
            {
                writer.Write("(");
                for (int i = 0; i < multiPoint.NumGeometries; i++) 
                {
                    if (i > 0) writer.Write(", ");
                    AppendCoordinate((Coordinate) ((Point)multiPoint.GetGeometryN(i)).Coordinate, writer, multiPoint.PrecisionModel);
                }
                writer.Write(")");
            }
        }

        /// <summary>
        /// Converts a <c>MultiLineString</c> to &lt;MultiLineString Text
        /// format, then appends it to the writer.
        /// </summary>
        /// <param name="multiLineString">The <c>MultiLineString</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="indentFirst"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendMultiLineStringText(MultiLineString multiLineString, int level, bool indentFirst,
                                               TextWriter writer)
        {
            if (multiLineString.IsEmpty) 
                writer.Write("EMPTY");            
            else 
            {
                int level2 = level;
                bool doIndent = indentFirst;
                writer.Write("(");
                for (int i = 0; i < multiLineString.NumGeometries; i++) 
                {
                    if (i > 0) 
                    {
                        writer.Write(", ");
                        level2 = level + 1;
                        doIndent = true;
                    }
                    AppendLineStringText((LineString) multiLineString.GetGeometryN(i), level2, doIndent, writer);
                }
                writer.Write(")");
            }
        }

        /// <summary>
        /// Converts a <c>MultiPolygon</c> to &lt;MultiPolygon Text format,
        /// then appends it to the writer.
        /// </summary>
        /// <param name="multiPolygon">The <c>MultiPolygon</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendMultiPolygonText(MultiPolygon multiPolygon, int level, TextWriter writer)            
        {
            if (multiPolygon.IsEmpty) 
                writer.Write("EMPTY");            
            else 
            {
                int level2 = level;
                bool doIndent = false;
                writer.Write("(");
                for (int i = 0; i < multiPolygon.NumGeometries; i++) 
                {
                    if (i > 0) 
                    {
                        writer.Write(", ");
                        level2 = level + 1;
                        doIndent = true;
                    }
                    AppendPolygonText((Polygon) multiPolygon.GetGeometryN(i), level2, doIndent, writer);
                }
                writer.Write(")");
            }
        }

        /// <summary>
        /// Converts a <c>GeometryCollection</c> to GeometryCollectionText
        /// format, then appends it to the writer.
        /// </summary>
        /// <param name="geometryCollection">The <c>GeometryCollection</c> to process.</param>
        /// <param name="level"></param>
        /// <param name="writer">The output writer to append to.</param>
        private void AppendGeometryCollectionText(GeometryCollection geometryCollection, int level, TextWriter writer)            
        {
            if (geometryCollection.IsEmpty)
                writer.Write("EMPTY");            
            else 
            {
                int level2 = level;
                writer.Write("(");
                for (int i = 0; i < geometryCollection.NumGeometries; i++) 
                {
                    if (i > 0) 
                    {
                        writer.Write(", ");
                        level2 = level + 1;
                    }
                    AppendGeometryTaggedText((Geometry) geometryCollection.GetGeometryN(i), level2, writer);
                }
                writer.Write(")");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="writer"></param>
        private void Indent(int level, TextWriter writer)
        {
            if (! isFormatted || level <= 0) return;
            writer.Write("\n");
            writer.Write(StringOfChar(' ', WKTWriterIndent * level));
        }
    }
}
