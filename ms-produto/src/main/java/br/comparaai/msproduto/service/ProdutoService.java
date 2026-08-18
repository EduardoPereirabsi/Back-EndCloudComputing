package br.comparaai.msproduto.service;

import br.comparaai.msproduto.domain.Produto;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

@Service
public class ProdutoService {

    private final ConcurrentHashMap<Long, Produto> produtos = new ConcurrentHashMap<>();
    private final AtomicLong sequence = new AtomicLong(0);

    public List<Produto> listar() {
        return new ArrayList<>(produtos.values());
    }

    public Optional<Produto> buscar(Long id) {
        return Optional.ofNullable(produtos.get(id));
    }

    public Produto salvar(Produto produto) {
        Long id = sequence.incrementAndGet();
        produto.setId(id);
        produtos.put(id, produto);
        return produto;
    }

    public Optional<Produto> atualizar(Long id, Produto dados) {
        Produto atual = produtos.get(id);
        if (atual == null) {
            return Optional.empty();
        }

        atual.setNome(dados.getNome());
        atual.setMarca(dados.getMarca());
        atual.setCategoria(dados.getCategoria());
        atual.setEan(dados.getEan());
        atual.setDescricao(dados.getDescricao());
        atual.setAtivo(dados.isAtivo());
        produtos.put(id, atual);

        return Optional.of(atual);
    }

    public boolean deletar(Long id) {
        return produtos.remove(id) != null;
    }
}
