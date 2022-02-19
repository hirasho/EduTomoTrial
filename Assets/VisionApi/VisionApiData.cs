using System.Collections.Generic;
using System;

namespace VisionApi
{
	[Serializable]
	public class BatchAnnotateImagesResponse {
		public List<AnnotateImageResponse> responses;
	}

	[Serializable]
	public class AnnotateImageResponse {
		public List<EntityAnnotation> landmarkAnnotations;
		public List<EntityAnnotation> logoAnnotations;
		public List<EntityAnnotation> labelAnnotations;
		public List<EntityAnnotation> textAnnotations;
		public TextAnnotation fullTextAnnotation;
		public State error;

		// まだある
	}

	[Serializable]
	public class State
	{
		public long code;
		public string message;
		// まだある
	}

	[Serializable]
	public class TextAnnotation
	{
		public List<Page> pages;
		public string text;
	}

	[Serializable]
	public class Page
	{
		public TextProperty property;
		public int width;
		public int height;
		public List<Block> blocks;
		public double confidence;
	}

	[Serializable]
	public class Block
	{
		public TextProperty property;
		public BoundingPoly boundingBox;
		public List<Paragraph> paragraphs;
		public BlockType blockType;
		public double confidence;	
	}

	public enum BlockType
	{
		UNKNOWN,
		TEXT,
		TABLE,
		PICTURE,
		RULER,
		BARCODE,
	}

	[Serializable]
	public class Paragraph
	{
		public TextProperty property;
		public BoundingPoly boundingBox;
		public List<Word> words;
		public double confidence;
	}

	[Serializable]
	public class Word
	{
		public TextProperty property;
		public BoundingPoly boundingBox;
		public List<Symbol> symbols;
		public double confidence;
	}

	[Serializable]
	public class Symbol
	{
		public TextProperty property;
		public BoundingPoly boundingBox;
		public string text;
		public double confidence;
	}


	[Serializable]
	public class TextProperty
	{
		public DetectedLanguage detectedLanguages;
		public DetectedBreak detectedBreak;	
	}

	public enum BreakType
	{
		UNKNOWN,
		SPACE,
		SURE_SPACE,
		EOL_SURE_SPACE,
		HYPHEN,
		LINE_BREAK,
	}


	[Serializable]
	public class DetectedBreak
	{
		public BreakType type;
		public bool isPrefix;		
	}

	[Serializable]
	public class DetectedLanguage
	{
		public string languageCode;
		public double confidence;
	}

	[Serializable]
	public class EntityAnnotation
	{
		public string mid;
		public string locale;
		public string description;
		public double score;
		public double topicality;
		public BoundingPoly boundingPoly;
		public List<LocationInfo> locations;
		public List<Property> properties;
	}

	[Serializable]
	public class BoundingPoly 
	{
		public List<Vertex> vertices;
	}

	[Serializable]
	public class Vertex 
	{
		public float x;
		public float y;
	}

	[Serializable]
	public class LocationInfo 
	{
		LatLng latLng;
	}

	[Serializable]
	public class Property 
	{
		string name;
		string value;
	}

	[Serializable]
	public class LatLng
	{
		public float latitude;
		public float longitude;
	}
}