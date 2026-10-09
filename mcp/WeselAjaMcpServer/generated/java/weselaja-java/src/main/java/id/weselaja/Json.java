package id.weselaja;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

final class Json {
    private Json() {
    }

    static String stringify(Object value) {
        StringBuilder output = new StringBuilder();
        write(value, output);
        return output.toString();
    }

    static Object parse(String input) {
        Parser parser = new Parser(input);
        Object value = parser.value();
        parser.whitespace();
        if (!parser.finished()) {
            throw parser.error("Unexpected content after JSON value");
        }
        return value;
    }

    private static void write(Object value, StringBuilder output) {
        if (value == null) {
            output.append("null");
        } else if (value instanceof String text) {
            quote(text, output);
        } else if (value instanceof Number || value instanceof Boolean) {
            output.append(value);
        } else if (value instanceof Map<?, ?> map) {
            output.append('{');
            boolean first = true;
            for (Map.Entry<?, ?> entry : map.entrySet()) {
                if (!(entry.getKey() instanceof String key)) {
                    throw new IllegalArgumentException("JSON object keys must be strings.");
                }
                if (!first) output.append(',');
                quote(key, output);
                output.append(':');
                write(entry.getValue(), output);
                first = false;
            }
            output.append('}');
        } else if (value instanceof Iterable<?> iterable) {
            output.append('[');
            boolean first = true;
            for (Object item : iterable) {
                if (!first) output.append(',');
                write(item, output);
                first = false;
            }
            output.append(']');
        } else {
            throw new IllegalArgumentException("Unsupported JSON value type: " + value.getClass().getName());
        }
    }

    private static void quote(String value, StringBuilder output) {
        output.append('"');
        for (int index = 0; index < value.length(); index++) {
            char character = value.charAt(index);
            switch (character) {
                case '"' -> output.append("\\\"");
                case '\\' -> output.append("\\\\");
                case '\b' -> output.append("\\b");
                case '\f' -> output.append("\\f");
                case '\n' -> output.append("\\n");
                case '\r' -> output.append("\\r");
                case '\t' -> output.append("\\t");
                default -> {
                    if (character < 0x20) {
                        output.append(String.format("\\u%04x", (int) character));
                    } else {
                        output.append(character);
                    }
                }
            }
        }
        output.append('"');
    }

    private static final class Parser {
        private final String input;
        private int position;

        private Parser(String input) {
            this.input = input;
        }

        private Object value() {
            whitespace();
            if (finished()) throw error("Expected JSON value");
            return switch (input.charAt(position)) {
                case '{' -> object();
                case '[' -> array();
                case '"' -> string();
                case 't' -> literal("true", Boolean.TRUE);
                case 'f' -> literal("false", Boolean.FALSE);
                case 'n' -> literal("null", null);
                default -> number();
            };
        }

        private Map<String, Object> object() {
            position++;
            whitespace();
            Map<String, Object> result = new LinkedHashMap<>();
            if (take('}')) return result;
            while (true) {
                whitespace();
                if (finished() || input.charAt(position) != '"') throw error("Expected object key");
                String key = string();
                whitespace();
                expect(':');
                result.put(key, value());
                whitespace();
                if (take('}')) return result;
                expect(',');
            }
        }

        private List<Object> array() {
            position++;
            whitespace();
            List<Object> result = new ArrayList<>();
            if (take(']')) return result;
            while (true) {
                result.add(value());
                whitespace();
                if (take(']')) return result;
                expect(',');
            }
        }

        private String string() {
            expect('"');
            StringBuilder result = new StringBuilder();
            while (!finished()) {
                char character = input.charAt(position++);
                if (character == '"') return result.toString();
                if (character < 0x20) throw error("Unescaped control character in string");
                if (character != '\\') {
                    result.append(character);
                    continue;
                }
                if (finished()) throw error("Incomplete escape sequence");
                char escape = input.charAt(position++);
                switch (escape) {
                    case '"', '\\', '/' -> result.append(escape);
                    case 'b' -> result.append('\b');
                    case 'f' -> result.append('\f');
                    case 'n' -> result.append('\n');
                    case 'r' -> result.append('\r');
                    case 't' -> result.append('\t');
                    case 'u' -> result.append(unicode());
                    default -> throw error("Invalid escape sequence");
                }
            }
            throw error("Unterminated string");
        }

        private char unicode() {
            if (position + 4 > input.length()) throw error("Incomplete Unicode escape");
            try {
                char result = (char) Integer.parseInt(input.substring(position, position + 4), 16);
                position += 4;
                return result;
            } catch (NumberFormatException exception) {
                throw error("Invalid Unicode escape");
            }
        }

        private Object number() {
            int start = position;
            if (take('-') && finished()) throw error("Invalid number");
            if (take('0')) {
                if (!finished() && Character.isDigit(input.charAt(position))) throw error("Leading zero in number");
            } else {
                digits();
            }
            if (take('.')) digits();
            if (take('e') || take('E')) {
                if (!take('+')) take('-');
                digits();
            }
            if (start == position) throw error("Invalid JSON value");
            try {
                return new BigDecimal(input.substring(start, position));
            } catch (NumberFormatException exception) {
                throw error("Invalid number");
            }
        }

        private void digits() {
            int start = position;
            while (!finished() && Character.isDigit(input.charAt(position))) position++;
            if (start == position) throw error("Expected digit");
        }

        private Object literal(String token, Object result) {
            if (!input.startsWith(token, position)) throw error("Invalid JSON literal");
            position += token.length();
            return result;
        }

        private void whitespace() {
            while (!finished() && Character.isWhitespace(input.charAt(position))) position++;
        }

        private boolean take(char expected) {
            if (!finished() && input.charAt(position) == expected) {
                position++;
                return true;
            }
            return false;
        }

        private void expect(char expected) {
            if (!take(expected)) throw error("Expected '" + expected + "'");
        }

        private boolean finished() {
            return position >= input.length();
        }

        private IllegalArgumentException error(String message) {
            return new IllegalArgumentException(message + " at JSON offset " + position);
        }
    }
}
