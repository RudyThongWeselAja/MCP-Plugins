package id.weselaja;

import java.util.Locale;
import java.util.Map;

public final class BankChannels {
    private static final Map<String, String> CHANNELS = Map.ofEntries(
            Map.entry("BRI", "BRI.VA"),
            Map.entry("BANKBRI", "BRI.VA"),
            Map.entry("MANDIRI", "MDR.VA"),
            Map.entry("BANKMANDIRI", "MDR.VA"),
            Map.entry("MDR", "MDR.VA"),
            Map.entry("BNI", "BNI.VA"),
            Map.entry("BANKBNI", "BNI.VA"),
            Map.entry("PERMATA", "PTB.VA"),
            Map.entry("BANKPERMATA", "PTB.VA"),
            Map.entry("PTB", "PTB.VA"),
            Map.entry("CIMB", "CIMB.VA"),
            Map.entry("CIMBNIAGA", "CIMB.VA"),
            Map.entry("BANKCIMB", "CIMB.VA"),
            Map.entry("BCA", "BCA.VA"),
            Map.entry("BANKBCA", "BCA.VA"));

    private BankChannels() {
    }

    public static String resolve(String bank) {
        if (bank == null || bank.isBlank()) {
            throw new IllegalArgumentException("Bank is required.");
        }
        String normalized = bank.trim().toUpperCase(Locale.ROOT).replaceAll("[ _-]", "");
        String channel = CHANNELS.get(normalized);
        if (channel == null) {
            throw new IllegalArgumentException(
                    "Unsupported bank '" + bank + "'. Supported banks: BRI, Mandiri, BNI, Permata, CIMB, BCA.");
        }
        return channel;
    }

    public static Map<String, String> supported() {
        return Map.of(
                "BRI", "BRI.VA",
                "Mandiri", "MDR.VA",
                "BNI", "BNI.VA",
                "Permata", "PTB.VA",
                "CIMB", "CIMB.VA",
                "BCA", "BCA.VA");
    }
}
