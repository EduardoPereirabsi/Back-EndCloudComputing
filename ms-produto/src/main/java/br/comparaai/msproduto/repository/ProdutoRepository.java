package br.comparaai.msproduto.repository;

import br.comparaai.msproduto.domain.Produto;
import org.springframework.data.jpa.repository.JpaRepository;

public interface ProdutoRepository extends JpaRepository<Produto, Long> {
}
