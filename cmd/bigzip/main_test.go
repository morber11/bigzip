package main

import (
	"bytes"
	"os"
	"path/filepath"
	"testing"
)

func TestWriteHeader(t *testing.T) {
	var buf bytes.Buffer
	h := &header{
		Magic:        [8]byte{'B', 'I', 'G', 'Z', 'I', 'P', '1', '1'},
		OriginalSize: 12345,
		Mode:         1,
		Reserved:     [7]byte{},
	}

	err := writeHeader(&buf, h)
	if err != nil {
		t.Fatalf("writeHeader failed: %v", err)
	}

	if buf.Len() != 24 {
		t.Errorf("expected header size 24, got %d", buf.Len())
	}
}

func TestReadHeader(t *testing.T) {
	buf := bytes.NewReader([]byte{
		'B', 'I', 'G', 'Z', 'I', 'P', '1', '1', // magic
		0x39, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 12345 little endian
		1,                   // mode
		0, 0, 0, 0, 0, 0, 0, // reserved
	})

	h, err := readHeader(buf)
	if err != nil {
		t.Fatalf("readHeader failed: %v", err)
	}

	if string(h.Magic[:]) != "BIGZIP11" {
		t.Errorf("expected magic BIGZIP11, got %s", string(h.Magic[:]))
	}

	if h.OriginalSize != 12345 {
		t.Errorf("expected OriginalSize 12345, got %d", h.OriginalSize)
	}

	if h.Mode != 1 {
		t.Errorf("expected Mode 1, got %d", h.Mode)
	}
}

func TestReadHeaderInvalidMagic(t *testing.T) {
	buf := bytes.NewReader([]byte{
		'I', 'N', 'V', 'A', 'L', 'I', 'D', '!', // invalid magic
		0, 0, 0, 0, 0, 0, 0, 0,
		0,
		0, 0, 0, 0, 0, 0, 0,
	})

	_, err := readHeader(buf)
	if err == nil {
		t.Error("expected error for invalid magic")
	}
}

func TestWriteRepeat(t *testing.T) {
	var buf bytes.Buffer
	inData := []byte("hello")
	remaining := int64(10)

	err := writeRepeat(&buf, inData, remaining)
	if err != nil {
		t.Fatalf("writeRepeat failed: %v", err)
	}

	output := buf.Bytes()
	if len(output) != 10 {
		t.Errorf("expected 10 bytes, got %d", len(output))
	}

	// should repeat "hello" twice: h e l l o h e l l o
	expected := []byte("hellohello")
	if !bytes.Equal(output, expected) {
		t.Errorf("expected %q, got %q", expected, output)
	}
}

func TestWritePadZero(t *testing.T) {
	var buf bytes.Buffer
	remaining := int64(5)

	err := writePad(&buf, remaining, false)
	if err != nil {
		t.Fatalf("writePad failed: %v", err)
	}

	output := buf.Bytes()
	if len(output) != 5 {
		t.Errorf("expected 5 bytes, got %d", len(output))
	}

	for _, b := range output {
		if b != 0 {
			t.Errorf("expected zero, got %d", b)
		}
	}
}

func TestWritePadRandom(t *testing.T) {
	var buf bytes.Buffer
	remaining := int64(5)

	err := writePad(&buf, remaining, true)
	if err != nil {
		t.Fatalf("writePad failed: %v", err)
	}

	output := buf.Bytes()
	if len(output) != 5 {
		t.Errorf("expected 5 bytes, got %d", len(output))
	}

	// it could potentially be all zeros so we need to handle this edge case
	allZero := true
	for _, b := range output {
		if b != 0 {
			allZero = false
			break
		}
	}
	if allZero {
		t.Error("random padding should not be all zeros")
	}
}

func TestUnbigzip(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "bigzip_test")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	originalPath := filepath.Join(tempDir, "original.txt")
	bigzipPath := filepath.Join(tempDir, "original.txt.bigzip")
	restoredPath := filepath.Join(tempDir, "restored.txt")

	originalData := []byte("This is the original content.")
	err = os.WriteFile(originalPath, originalData, 0644)
	if err != nil {
		t.Fatalf("failed to write original file: %v", err)
	}

	outFile, err := os.Create(bigzipPath)
	if err != nil {
		t.Fatalf("failed to create bigzip file: %v", err)
	}

	h := header{
		Magic:        [8]byte{'B', 'I', 'G', 'Z', 'I', 'P', '1', '1'},
		OriginalSize: uint64(len(originalData)),
		Mode:         0,
	}
	err = writeHeader(outFile, &h)
	if err != nil {
		t.Fatalf("failed to write header: %v", err)
	}

	_, err = outFile.Write(originalData)
	if err != nil {
		t.Fatalf("failed to write original data: %v", err)
	}

	padding := []byte("padding")
	_, err = outFile.Write(padding)
	if err != nil {
		t.Fatalf("failed to write padding: %v", err)
	}

	outFile.Close()

	_, err = unbigzip(bigzipPath, restoredPath)
	if err != nil {
		t.Fatalf("unbigzip failed: %v", err)
	}

	restoredData, err := os.ReadFile(restoredPath)
	if err != nil {
		t.Fatalf("failed to read restored file: %v", err)
	}

	if !bytes.Equal(restoredData, originalData) {
		t.Errorf("restored data does not match original: got %q, want %q", restoredData, originalData)
	}
}

func TestWriteAll(t *testing.T) {
	var buf bytes.Buffer
	data := []byte("hello world")

	err := writeAll(&buf, data)
	if err != nil {
		t.Fatalf("writeAll failed: %v", err)
	}

	if !bytes.Equal(buf.Bytes(), data) {
		t.Errorf("writeAll did not write correct data")
	}
}

func TestOverwriteProtection(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "bigzip_overwrite_test")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	originalPath := filepath.Join(tempDir, "original.txt")
	bigzipPath := filepath.Join(tempDir, "original.txt.bigzip")
	restoredPath := filepath.Join(tempDir, "restored.txt")

	originalData := []byte("This is the original content.")
	err = os.WriteFile(originalPath, originalData, 0644)
	if err != nil {
		t.Fatalf("failed to write original file: %v", err)
	}

	cfg := &config{
		input:  originalPath,
		output: bigzipPath,
		factor: 2.0,
		mode:   "repeat",
		unbig:  false,
		force:  false,
	}
	err = run(cfg)
	if err != nil {
		t.Fatalf("failed to create bigzip: %v", err)
	}

	err = os.WriteFile(restoredPath, []byte("existing"), 0644)
	if err != nil {
		t.Fatalf("failed to create existing restored file: %v", err)
	}

	cfgUnbig := &config{
		input:  bigzipPath,
		output: restoredPath,
		unbig:  true,
		force:  false,
	}
	err = run(cfgUnbig)
	if err != nil {
		t.Fatalf("failed to unbigzip with auto-increment: %v", err)
	}

	restoredPath1 := filepath.Join(tempDir, "restored_1.txt")
	restoredData, err := os.ReadFile(restoredPath1)
	if err != nil {
		t.Fatalf("failed to read auto-incremented restored file: %v", err)
	}
	if !bytes.Equal(restoredData, originalData) {
		t.Errorf("auto-incremented restored data does not match original: got %q, want %q", restoredData, originalData)
	}

	cfgUnbigForce := &config{
		input:  bigzipPath,
		output: restoredPath,
		unbig:  true,
		force:  true,
	}
	err = run(cfgUnbigForce)
	if err != nil {
		t.Fatalf("failed to unbigzip with force: %v", err)
	}

	restoredData, err = os.ReadFile(restoredPath)
	if err != nil {
		t.Fatalf("failed to read restored file: %v", err)
	}
	if !bytes.Equal(restoredData, originalData) {
		t.Errorf("restored data does not match original: got %q, want %q", restoredData, originalData)
	}

	cfgCompress := &config{
		input:  originalPath,
		output: bigzipPath,
		factor: 2.0,
		mode:   "repeat",
		unbig:  false,
		force:  false,
	}
	err = run(cfgCompress)

	if err != nil {
		t.Fatalf("failed to create auto-incremented bigzip: %v", err)
	}

	autoBigzip := filepath.Join(tempDir, "original_1.txt.bigzip")
	if _, err := os.Stat(autoBigzip); err != nil {
		t.Fatalf("expected auto-incremented bigzip %s to exist: %v", autoBigzip, err)
	}

	cfgCompressForce := &config{
		input:  originalPath,
		output: bigzipPath,
		factor: 2.0,
		mode:   "repeat",
		unbig:  false,
		force:  true,
	}
	err = run(cfgCompressForce)
	if err != nil {
		t.Fatalf("failed to compress with force: %v", err)
	}
}
