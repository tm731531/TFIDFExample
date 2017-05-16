using EnglishStemmer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TFIDFExample
{
    /// <summary>
    /// Copyright (c) 2013 Kory Becker http://www.primaryobjects.com/kory-becker.aspx
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining
    /// a copy of this software and associated documentation files (the
    /// "Software"), to deal in the Software without restriction, including
    /// without limitation the rights to use, copy, modify, merge, publish,
    /// distribute, sublicense, and/or sell copies of the Software, and to
    /// permit persons to whom the Software is furnished to do so, subject to
    /// the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be
    /// included in all copies or substantial portions of the Software.
    /// 
    /// Description:
    /// Performs a TF*IDF (Term Frequency * Inverse Document Frequency) transformation on an array of documents.
    /// Each document string is transformed into an array of doubles, cooresponding to their associated TF*IDF values.
    /// 
    /// Usage:
    /// string[] documents = LoadYourDocuments();
    ///
    /// double[][] inputs = TFIDF.Transform(documents);
    /// inputs = TFIDF.Normalize(inputs);
    /// 
    /// </summary>
    public static class TFIDF
    {
        /// <summary>
        /// Document vocabulary, containing each word's IDF value.
        /// </summary>
        public static Dictionary<string, double> _vocabularyIDF = new Dictionary<string, double>();

        /// <summary>
        /// Transforms a list of documents into their associated TF*IDF values.
        /// If a vocabulary does not yet exist, one will be created, based upon the documents' words.
        /// </summary>
        /// <param name="documents">string[]</param>
        /// <param name="vocabularyThreshold">Minimum number of occurences of the term within all documents</param>
        /// <returns>double[][]</returns>
        public static void Transform(string[] documents, int vocabularyThreshold = 3)
        {
            List<List<string>> stemmedDocs;
            List<string> vocabulary;

            // Get the vocabulary and stem the documents at the same time.
            vocabulary = GetVocabulary(documents, out stemmedDocs, vocabularyThreshold);

            if (_vocabularyIDF.Count == 0)
            {
                // Calculate the IDF for each vocabulary term.
                foreach (var term in vocabulary)
                {
                    double numberOfDocsContainingTerm = stemmedDocs.Where(d => d.Contains(term)).Count();
                    _vocabularyIDF[term] = Math.Log((double)stemmedDocs.Count / ((double)1 + numberOfDocsContainingTerm));
                }
            }

            // Transform each document into a vector of tfidf values.
          TransformToTFIDFVectorsBigList(stemmedDocs, _vocabularyIDF);
          //  return TransformToTFIDFVectors(stemmedDocs, _vocabularyIDF);
        }

        /// <summary>
        /// Converts a list of stemmed documents (lists of stemmed words) and their associated vocabulary + idf values, into an array of TF*IDF values.
        /// </summary>
        /// <param name="stemmedDocs">List of List of string</param>
        /// <param name="vocabularyIDF">Dictionary of string, double (term, IDF)</param>
        /// <returns>double[][]</returns>
        private static  List<List<double>> TransformToTFIDFVectors(List<List<string>> stemmedDocs, Dictionary<string, double> vocabularyIDF)
        {
            // Transform each document into a vector of tfidf values.
            List<List<double>> vectors = new List<List<double>>();
            foreach (var doc in stemmedDocs)
            {
                List<double> vector = new List<double>();

                foreach (var vocab in vocabularyIDF)
                {
                    // Term frequency = count how many times the term appears in this document.
                    //double tf = doc.Where(d => d == vocab.Key).Count();
                    //double tfidf = doc.Where(d => d == vocab.Key).Count() * vocab.Value;

                    vector.Add(doc.Where(d => d == vocab.Key).Count() * vocab.Value);
                    //Console.WriteLine("TransformToTFIDFVectors " + vocab.Key  + "\n");
                }

                vectors.Add(vector);
            }

            //return vectors.Select(v => v.ToArray()).ToArray();
            return vectors;
        }
        private static void TransformToTFIDFVectorsBigList(List<List<string>> stemmedDocs, Dictionary<string, double> vocabularyIDF )
        {
            // Transform each document into a vector of tfidf values.
            List<List<double>> vectors = new List<List<double>>();
           List<string> vectorsString = new List<string>();
            foreach (var docc in stemmedDocs)
            {
                foreach (var docstring in docc)
                {
                    vectorsString.Add(docstring);
                }
            }
            //  vectorsString = vectorsString.Distinct().ToList();
            // foreach (var doc in vectorsString)
            SqlConnection connection = new SqlConnection(Program.conn);

            string targetTable = "TFIDF";
            SqlDataAdapter adapter = new SqlDataAdapter("SELECT top(0) * FROM " + targetTable, Program.conn);
            DataTable datatable = new DataTable();
            adapter.Fill(datatable);
            SqlBulkCopy SBC = new SqlBulkCopy(connection);
            SBC.BulkCopyTimeout = 0;
            SBC.DestinationTableName = "dbo." + targetTable;

            List<object> colData = new List<object>();
            connection.Open();
            foreach (var vocab in vocabularyIDF)
            {
                colData.Clear();
                colData.Add(null);
                colData.Add(null);
                colData.Add(vocab.Key);
                colData.Add(vectorsString.Where(d => d == vocab.Key).Count() * vocab.Value);
                datatable.Rows.Add(colData.ToArray());

              //  Console.WriteLine(value.Key + "  :  " + value.Value + "\n");
                Console.Write(vocab.Key + "\n");
            }
            SBC.WriteToServer(datatable);
            connection.Close();

            //{
            //    List<double> vector = new List<double>();

            //    foreach (var vocab in vocabularyIDF)
            //    {
            //        // Term frequency = count how many times the term appears in this document.
            //        //double tf = doc.Where(d => d == vocab.Key).Count();
            //        //double tfidf = doc.Where(d => d == vocab.Key).Count() * vocab.Value;

            //        vector.Add(vectorsString.Where(d => d == vocab.Key).Count() * vocab.Value);
            //        //Console.WriteLine("TransformToTFIDFVectors " + vocab.Key  + "\n");
            //    }

            //    vectors.Add(vector);
            //}

            //return vectors.Select(v => v.ToArray()).ToArray();
            //return vectors;
        }
        /// <summary>
        /// Normalizes a TF*IDF array of vectors using L2-Norm.
        /// Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
        /// </summary>
        /// <param name="vectors">double[][]</param>
        /// <returns>double[][]</returns>
        public static List<List<double>> Normalize(List<List<double>> vectors)
        {
            // Normalize the vectors using L2-Norm.
            List<List<double>> normalizedVectors = new List<List<double>>();
            int docIndex = 0;
            foreach (var vector in vectors)
            {


                if (docIndex % 100 == 0)
                {
                    Console.WriteLine("Normalize " + docIndex + "/" + vectors.Count+"\n");
                }
                 normalizedVectors.Add(Normalize(vector));
            }

            return normalizedVectors;
        }

        /// <summary>
        /// Normalizes a TF*IDF vector using L2-Norm.
        /// Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
        /// </summary>
        /// <param name="vectors">double[][]</param>
        /// <returns>double[][]</returns>
        public static List<double> Normalize(List<double> vector)
        {
            List<double> result = new List<double>();

            double sumSquared = 0;
            foreach (var value in vector)
            {
                sumSquared += value * value;
            }

            double SqrtSumSquared = Math.Sqrt(sumSquared);

            foreach (var value in vector)
            {
                // L2-norm: Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
                result.Add(value / SqrtSumSquared);
            }

            return result;
        }

        /// <summary>
        /// Saves the TFIDF vocabulary to disk.
        /// </summary>
        /// <param name="filePath">File path</param>
        public static void Save(string filePath = "vocabulary.dat")
        {
            // Save result to disk.
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, _vocabularyIDF);
            }
        }

        /// <summary>
        /// Loads the TFIDF vocabulary from disk.
        /// </summary>
        /// <param name="filePath">File path</param>
        public static void Load(string filePath = "vocabulary.dat")
        {
            // Load from disk.
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                _vocabularyIDF = (Dictionary<string, double>)formatter.Deserialize(fs);
            }
        }

        #region Private Helpers

        /// <summary>
        /// Parses and tokenizes a list of documents, returning a vocabulary of words.
        /// </summary>
        /// <param name="docs">string[]</param>
        /// <param name="stemmedDocs">List of List of string</param>
        /// <returns>Vocabulary (list of strings)</returns>
        private static List<string> GetVocabulary(string[] docs, out List<List<string>> stemmedDocs, int vocabularyThreshold)
        {
            List<string> vocabulary = new List<string>();
            Dictionary<string, int> wordCountList = new Dictionary<string, int>();
            stemmedDocs = new List<List<string>>();

            int docIndex = 0;

            foreach (var doc in docs)
            {
                List<string> stemmedDoc = new List<string>();

                docIndex++;

                if (docIndex % 100 == 0)
                {
                    Console.WriteLine("Processing " + docIndex + "/" + docs.Length);
                }

                string[] parts2 = Tokenize(doc);

                List<string> words = new List<string>();
                foreach (string part in parts2)
                {
                    // Strip non-alphanumeric characters.
                    //string stripped = Regex.Replace(part, "[^a-zA-Z0-9]", "");

                    if (!StopWords.stopWordsList.Contains(part.ToLower()))
                    {
                        try
                        {
                            //var english = new EnglishWord(stripped);
                           // string stem = english.Stem;
                            string stem = part.ToLower();
                            words.Add(stem);

                            if (stem.Length > 0)
                            {
                                // Build the word count list.
                                if (wordCountList.ContainsKey(stem))
                                {
                                    wordCountList[stem]++;
                                }
                                else
                                {
                                    wordCountList.Add(stem, 0);
                                }

                                stemmedDoc.Add(stem);
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                stemmedDocs.Add(stemmedDoc);
            }

            // Get the top words.
            var vocabList = wordCountList.Where(w => w.Value >= vocabularyThreshold);
            foreach (var item in vocabList)
            {
                vocabulary.Add(item.Key);
            }

            return vocabulary;
        }

        /// <summary>
        /// Tokenizes a string, returning its list of words.
        /// </summary>
        /// <param name="text">string</param>
        /// <returns>string[]</returns>
        private static string[] Tokenize(string text)
        {
            // Strip all HTML.
            text = Regex.Replace(text, "<[^<>]+>", "");

            // Strip numbers.
           // text = Regex.Replace(text, "[0-9]+", "number");

            // Strip urls.
            text = Regex.Replace(text, @"(http|https)://[^\s]*", "");

            // Strip email addresses.
            //  text = Regex.Replace(text, @"[^\s]+@[^\s]+", "emailaddr");

            // Strip dollar sign.
            text = Regex.Replace(text, "[$]+", "");

            // Strip mutiple white space sign.
            text = Regex.Replace(text, "\\s+", " ");


            // Strip usernames.
            // text = Regex.Replace(text, @"@[^\s]+", "username");

            // Tokenize and also get rid of any punctuation
          //  return text.Split(" @$/#.-:&*+=[]?!(){},''\">_<;%\\".ToCharArray());
            return text.Split(" @$/#-:&*+=[]?!(){},''\"><;%\\".ToCharArray());
            //  return text.Split(" ".ToCharArray());
        }

        #endregion
    }
}
