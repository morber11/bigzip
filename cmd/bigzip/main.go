package main

import (
	"crypto/rand"
	"encoding/binary"
	"errors"
	"flag"
	"fmt"
	"io"
	"math"
	"os"
	"path/filepath"
	"strings"
)

const magic = "BIGZIP11" // there is no special meaning to 11 here

// header struct is 24 bytes: 8 magic, 8 original size, 1 mode, 7 reserved
type header struct {
	Magic        [8]byte
	OriginalSize uint64
	Mode         uint8 // 0: repeat, 1: zero, 2: random
	Reserved     [7]byte
}

type config struct {
	input  string
	output string
	factor float64
	mode   string
	unbig  bool
	force  bool
}

func main() {
	cfg := parseFlags()
	if err := run(cfg); err != nil {
		fmt.Fprintf(os.Stderr, "error: %v\n", err)
		os.Exit(1)
	}
}

// TODO: split this into smaller funcs later
func run(cfg *config) error {
	if cfg.input == "" {
		return errors.New("-input or -i is required")
	}

	inFile, err := os.Open(cfg.input)
	if err != nil {
		return fmt.Errorf("failed to open input: %w", err)
	}

	defer inFile.Close()

	if cfg.unbig {
		output := cfg.output
		if output == "" {
			if strings.HasSuffix(cfg.input, ".bigzip") {
				output = strings.TrimSuffix(cfg.input, ".bigzip")
			} else {
				output = cfg.input + ".orig"
			}
		}

		if !cfg.force {
			if _, err := os.Stat(output); err == nil {
				output = findUniquePath(output)
			}
		}

		if mode, err := unbigzip(cfg.input, output); err != nil {
			return fmt.Errorf("unbigzip error: %w", err)
		} else {
			fmt.Printf("Restored original to %s (mode: %s)\n", output, modeString(mode))
		}

		return nil
	}

	info, err := inFile.Stat()
	if err != nil {
		return fmt.Errorf("failed to stat input: %w", err)
	}

	inSize := info.Size()

	if cfg.factor < 1.0 {
		return errors.New("-factor must be >= 1.0")
	}

	desiredTotal := int64(math.Ceil(float64(inSize) * cfg.factor))

	// must account for header + original size at minimum
	minTotal := int64(24) + inSize

	if desiredTotal < minTotal {
		desiredTotal = minTotal
	}

	inData, err := os.ReadFile(cfg.input)
	if err != nil {
		return fmt.Errorf("failed to read input: %w", err)
	}

	outPath := cfg.output
	if outPath == "" {
		outPath = filepath.Join(filepath.Dir(cfg.input), filepath.Base(cfg.input)+".bigzip")
	}

	if !cfg.force {
		if _, err := os.Stat(outPath); err == nil {
			outPath = findUniquePath(outPath)
		}
	}

	outFile, err := os.Create(outPath)
	if err != nil {
		return fmt.Errorf("failed to create output: %w", err)
	}

	defer func() {
		cerr := outFile.Close()
		if cerr != nil {
			fmt.Fprintf(os.Stderr, "warning: closing output: %v\n", cerr)
		}
	}()

	m := strings.ToLower(cfg.mode)

	var modeByte uint8
	switch m {
	case "repeat":
		modeByte = 0
	case "zero":
		modeByte = 1
	case "random":
		modeByte = 2
	default:
		return fmt.Errorf("unknown mode '%s'", cfg.mode)
	}

	// write header
	var hdr header

	copy(hdr.Magic[:], []byte(magic))
	hdr.OriginalSize = uint64(len(inData))
	hdr.Mode = modeByte
	if err := writeHeader(outFile, &hdr); err != nil {
		return fmt.Errorf("failed to write header: %w", err)
	}

	// write the original content once
	if err := writeAll(outFile, inData); err != nil {
		return fmt.Errorf("failed to write body: %w", err)
	}

	// remaining space to reach desired total
	remaining := desiredTotal - (int64(24) + int64(len(inData)))

	switch m {
	case "repeat":
		if err := writeRepeat(outFile, inData, remaining); err != nil {
			return fmt.Errorf("inflation error: %w", err)
		}
	case "zero":
		if err := writePad(outFile, remaining, false); err != nil {
			return fmt.Errorf("inflation error: %w", err)
		}
	case "random":
		if err := writePad(outFile, remaining, true); err != nil {
			return fmt.Errorf("inflation error: %w", err)
		}
	}

	fmt.Printf("Wrote %s (size: %d bytes)\n", outPath, desiredTotal)
	return nil
}

func parseFlags() *config {
	inputLong := flag.String("input", "", "Input file path")
	inputShort := flag.String("i", "", "Input file path (short)")
	outputLong := flag.String("output", "", "Output file path (defaults to <name>.bigzip next to input)")
	outputShort := flag.String("o", "", "Output file path (short)")
	factorLong := flag.Float64("factor", 0.0, "Size multiplier")
	factorShort := flag.Float64("f", 0.0, "Size multiplier (short)")
	mode := flag.String("mode", "repeat", "Inflation mode: repeat|zero|random")
	unbigLong := flag.Bool("unbigzip", false, "If set, restores a .bigzip file to its original content")
	unbigShort := flag.Bool("uz", false, "If set, restores a .bigzip file to its original content (short)")
	forceLong := flag.Bool("force", false, "If set, allows overwriting existing output files")

	flag.Parse()

	var input, output string

	if *inputShort != "" {
		input = *inputShort
	} else {
		input = *inputLong
	}

	if *outputShort != "" {
		output = *outputShort
	} else {
		output = *outputLong
	}

	// numeric flags: if short non-zero use short; else use long; factor defaults to 5.0
	var factor float64
	if *factorShort != 0.0 {
		factor = *factorShort
	} else if *factorLong != 0.0 {
		factor = *factorLong
	} else {
		factor = 5.0
	}

	unbig := *unbigLong || *unbigShort
	force := *forceLong

	// in case no input is provided but unbig is set, assume first arg is input
	if unbig && input == "" {
		args := flag.Args()
		if len(args) > 0 {
			input = args[0]
		}
	}

	return &config{
		input:  input,
		output: output,
		factor: factor,
		mode:   *mode,
		unbig:  unbig,
		force:  force,
	}
}

func findUniquePath(path string) string {
	if _, err := os.Stat(path); os.IsNotExist(err) {
		return path
	}

	dir := filepath.Dir(path)
	base := filepath.Base(path)

	// special handling for .bigzip files: insert counter before .bigzip
	if strings.HasSuffix(base, ".bigzip") {
		nameWithoutBigzip := strings.TrimSuffix(base, ".bigzip")
		originalExt := filepath.Ext(nameWithoutBigzip)
		nameWithoutExt := strings.TrimSuffix(nameWithoutBigzip, originalExt)

		for i := 1; ; i++ {
			newName := fmt.Sprintf("%s_%d%s.bigzip", nameWithoutExt, i, originalExt)
			newPath := filepath.Join(dir, newName)
			if _, err := os.Stat(newPath); os.IsNotExist(err) {
				return newPath
			}
		}
	}

	ext := filepath.Ext(base)
	name := strings.TrimSuffix(base, ext)

	// get a suffix for the file e.g File_1 if no overwrite
	for i := 1; ; i++ {
		newName := fmt.Sprintf("%s_%d%s", name, i, ext)
		newPath := filepath.Join(dir, newName)
		if _, err := os.Stat(newPath); os.IsNotExist(err) {
			return newPath
		}
	}
}

func modeString(m uint8) string {
	switch m {
	case 0:
		return "repeat"
	case 1:
		return "zero"
	case 2:
		return "random"
	default:
		return "unknown"
	}
}

func writeAll(w io.Writer, b []byte) error {
	for len(b) > 0 {
		n, err := w.Write(b)
		if err != nil {
			return err
		}
		b = b[n:]
	}

	return nil
}

func writeRepeat(out io.Writer, inData []byte, remaining int64) error {
	if remaining <= 0 {
		return nil
	}

	chunk := inData

	if len(chunk) == 0 {
		chunk = []byte{0}
	}

	for remaining > 0 {
		toWrite := chunk

		if int64(len(toWrite)) > remaining {
			toWrite = toWrite[:remaining]
		}

		if err := writeAll(out, toWrite); err != nil {
			return err
		}
		remaining -= int64(len(toWrite))
	}

	return nil
}

func writePad(out io.Writer, remaining int64, random bool) error {
	if remaining <= 0 {
		return nil
	}

	bufSize := 64 * 1024
	buf := make([]byte, bufSize)

	for remaining > 0 {
		fill := buf
		if int64(len(fill)) > remaining {
			fill = fill[:remaining]
		}

		if random {
			if _, err := rand.Read(fill); err != nil {
				return err
			}
		} else {
			for i := range fill {
				fill[i] = 0
			}
		}

		if err := writeAll(out, fill); err != nil {
			return err
		}

		remaining -= int64(len(fill))
	}

	return nil
}

func writeHeader(w io.Writer, h *header) error {
	buf := make([]byte, 24)
	copy(buf[0:8], h.Magic[:])
	binary.LittleEndian.PutUint64(buf[8:16], h.OriginalSize)
	buf[16] = h.Mode
	// reserved already zero
	_, err := w.Write(buf)

	return err
}

func readHeader(r io.Reader) (*header, error) {
	buf := make([]byte, 24)
	if _, err := io.ReadFull(r, buf); err != nil {
		return nil, err
	}

	var h header
	copy(h.Magic[:], buf[0:8])

	h.OriginalSize = binary.LittleEndian.Uint64(buf[8:16])
	h.Mode = buf[16]

	if string(h.Magic[:]) != magic {
		return nil, errors.New("not a bigzip file")
	}

	if h.Mode > 2 {
		return nil, errors.New("invalid mode")
	}
	return &h, nil
}

func unbigzip(inPath, outPath string) (uint8, error) {
	in, err := os.Open(inPath)

	if err != nil {
		return 0, err
	}

	defer in.Close()
	h, err := readHeader(in)

	if err != nil {
		return 0, err
	}

	// copy original bytes out
	out, err := os.Create(outPath)
	if err != nil {
		return 0, err
	}

	defer func() {
		_ = out.Close()
	}()
	toCopy := int64(h.OriginalSize)

	// stream copy up to original size
	buf := make([]byte, 64*1024)
	for toCopy > 0 {
		n := int64(len(buf))
		if n > toCopy {
			n = toCopy
		}
		readBuf := buf[:n]
		rn, rerr := io.ReadFull(in, readBuf)
		if rerr != nil && rerr != io.ErrUnexpectedEOF {
			return 0, rerr
		}

		if rn == 0 {
			break
		}

		if _, werr := out.Write(readBuf[:rn]); werr != nil {
			return 0, werr
		}

		toCopy -= int64(rn)
	}

	return h.Mode, nil
}
