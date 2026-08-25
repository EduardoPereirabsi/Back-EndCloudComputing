// services/java/src/main/java/app/ApiController.java
package app;

import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.web.bind.annotation.*;
import java.net.InetAddress;
import java.util.List;
import java.util.Map;

@RestController
public class ApiController {
  private final String service = "ms-java";
  private final JdbcTemplate jdbc;

  public ApiController(JdbcTemplate jdbc) { this.jdbc = jdbc; }

  @GetMapping("/health")
  public Map<String, Object> health() throws Exception {
    return Map.of("status","ok","service", service,"hostname", InetAddress.getLocalHost().getHostName());
  }

  @GetMapping("/messages")
  public List<Map<String,Object>> list() {
    return jdbc.queryForList("SELECT id, text FROM messages ORDER BY id");
  }

  record MsgIn(String text) {}

  @PostMapping("/messages")
  public Map<String,Object> create(@RequestBody MsgIn in) {
    jdbc.update("INSERT INTO messages(text) VALUES (?)", in.text());
    Integer id = jdbc.queryForObject("SELECT MAX(id) FROM messages", Integer.class);
    return Map.of("id", id, "text", in.text());
  }
}
