# DIE: a PDF Intelligence API

A small ASP.NET Core Web API for uploading, processing, and extracting text from PDF documents.

The project is being developed incrementally, with the goal of eventually turning extracted PDF content into structured and useful information such as summaries, key points, document purpose, topics, and other metadata.

## Current Features

- Upload PDF documents
- Store uploaded PDFs locally
- Retrieve uploaded documents
- Get basic PDF information
- Detect text-based and image-based PDF pages
- Extract text directly from text-based pages using PdfPig
- Render image-based PDF pages using SkiaSharp
- OCR image-based pages using OCR.space
- Combine text extracted from both text and image pages
- Clean extracted text using regular expressions
- Return document information as JSON

## How It Works

The API processes each PDF page individually.

```text
                    ┌── Text-based page ──→ PdfPig ──────┐
PDF → Page Detection                                  ├──→ Combined Text
                    └── Image-based page → SkiaSharp → OCR.space
```

For text-based pages, the project extracts the text directly from the PDF.

For image-based pages, the page is rendered into a PNG image using SkiaSharp and sent to OCR.space for text recognition.

The resulting text from both methods is then combined and cleaned before being returned by the API.

## Technologies

- C#
- ASP.NET Core 8
- Minimal APIs
- PdfPig
- SkiaSharp
- OCR.space API
- Regular Expressions (Regex)

## API Endpoints

### Upload a PDF

```http
POST /api/docs
```

Accepts a PDF file and stores it in the `Docs` directory.

Current limitations:

- PDF files only
- Maximum file size: 50 MB

### List uploaded PDFs

```http
GET /api/docs
```

Returns the currently stored PDF documents.

### Get a PDF

```http
GET /api/docs/{id}
```

Returns a specific uploaded PDF.

### Get basic document information

```http
GET /api/docs/{id}/info
```

Returns basic information such as:

- Document name
- Number of pages
- PDF metadata title
- Author
- A short preview

### Process the entire document

```http
GET /api/docs/{id}/fullinfo
```

Processes the document page by page and returns:

- Document name
- Number of pages
- Number of text-based pages
- Number of image-based pages
- Text-based page numbers
- Image-based page numbers
- Extracted and cleaned text

## Example

A processed document can return information similar to:

```json
{
  "name": "example.pdf",
  "numPages": 5,
  "text": "My Document\nIntroduction\nThis document...",
  "numPicPages": 2,
  "numTextPages": 3,
  "picBasedPages": [2, 4],
  "textBasedPages": [1, 3, 5]
}
```

## OCR Configuration

The project uses OCR.space for image-based PDF pages.

The API key should **not** be stored directly in the source code.

For local development, configure the API key using ASP.NET Core User Secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "OCRSpace:ApiKey" "YOUR_API_KEY"
```

The key is then loaded through the application's configuration system.

> Never commit your OCR API key or other secrets to the repository.

## Project Structure

The project is currently organized around the following main components:

```text
├── Docs/
│   └── Uploaded PDF files
│
├── wwwroot/
│   └── Frontend/static files
│
├── DocumentEndpoints.cs
│   └── PDF upload and document endpoints
│
├── DocumentInfoEndpoints.cs
│   └── PDF processing, text extraction and OCR
│
├── DocInfo.cs
│   └── Document information model
│
└── Program.cs
    └── Application configuration and endpoint registration
```

## Current Processing Pipeline

```text
1. Upload PDF
       ↓
2. Open PDF with PdfPig
       ↓
3. Process each page
       ↓
4. Does the page contain extractable text?
       │
       ├── Yes → Extract text with PdfPig
       │
       └── No → Render page with SkiaSharp
                    ↓
                 PNG image
                    ↓
                 OCR.space
                    ↓
                 Extracted text
       ↓
5. Combine document text
       ↓
6. Clean text with Regex
       ↓
7. Return structured JSON
```

## Roadmap

The project is being developed incrementally.

### Completed

- [x] PDF upload
- [x] PDF storage
- [x] PDF retrieval
- [x] Basic PDF information
- [x] Page-by-page text/image detection
- [x] Direct PDF text extraction
- [x] Image page rendering
- [x] OCR integration
- [x] Combined text extraction
- [x] Extracted text cleaning
- [x] Cache processed documents
- [x] Store processed document results

### Next

- [ ] Avoid repeating OCR for already processed documents
- [ ] Improve document preview generation
- [ ] Add AI-powered document analysis
- [ ] Extract structured information such as:
  - [ ] Document title
  - [ ] Purpose
  - [ ] Topics/fields
  - [ ] Key points
  - [ ] Important sections
  - [ ] Summary

### Future Ideas

- [ ] Search within documents
- [ ] Ask questions about a document
- [ ] Return relevant pages for answers
- [ ] Document citations
- [ ] Support more advanced document analysis

## Why This Project?

The project started as a small experiment with ASP.NET Core and PDF processing and is being developed into a document-processing backend.

Instead of starting with a large architecture, the project is built incrementally: each new feature is added when the current implementation introduces a practical problem that needs to be solved.

This also makes the project a practical exploration of:

- REST APIs
- File handling
- PDF processing
- OCR
- External API integration
- Text processing
- Caching
- AI integration
- Backend architecture

## Status

🚧 **In development**

The project is currently focused on reliable PDF text extraction and OCR. AI-powered document analysis and caching are planned next.
