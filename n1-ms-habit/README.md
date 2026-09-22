# N1 — Integração do ms-habit ao Kubernetes

Roteiro para reproduzir o ambiente do zero e apresentar as três rubricas.

## Pré-requisitos

Cluster de pé e cliente kubectl compatível:

    minikube start --driver=docker
    minikube addons enable ingress
    $env:Path = "C:\Users\eduar\.minikube\cache\windows\amd64\v1.37.0;" + $env:Path

## 1. Subir a arquitetura

A imagem `eduardo1112/ms-habit:1.0.0` já está publicada no Docker Hub, então não é
preciso compilar nada em sala. Aplique na ordem:

    kubectl apply -f k8s/mysql-habit.yaml
    kubectl apply -f k8s/ms-habit.yaml
    kubectl apply -f ../ms-produto/k8s/mysql.yaml
    kubectl apply -f ../ms-produto/k8s/ms-produto.yaml
    kubectl apply -f k8s/ingress.yaml

## 2. Evidências de cada rubrica

### R1 — implantação e escalabilidade horizontal (3,0)

    kubectl get deployments
    kubectl get pods -l app=ms-habit

Esperado: `ms-habit 5/5` e cinco pods em Running, ao lado de `ms-produto`,
`mysql-habit` e `mysql-produto`.

### R2 — Service Discovery e Database per Service (3,0)

Acesso pelo NOME do Service, nunca por IP:

    kubectl run teste --image=curlimages/curl --restart=Never -- sleep 7200
    kubectl exec teste -- curl -s http://ms-habit:8081/habits
    kubectl exec teste -- curl -s http://ms-produto:8080/produto/produtos

Bancos separados (cada um só tem o seu schema):

    kubectl exec deploy/mysql-habit   -- mysql -uroot -proot -e "SHOW DATABASES;"
    kubectl exec deploy/mysql-produto -- mysql -uroot -proot -e "SHOW DATABASES;"
    kubectl exec deploy/mysql-habit   -- mysql -uroot -proot habitdb   -e "SELECT * FROM habits;"
    kubectl exec deploy/mysql-produto -- mysql -uroot -proot produtodb -e "SELECT * FROM produtos;"

### R3 — Ingress e API operacional (4,0)

Em um terminal separado, mantenha aberto:

    kubectl port-forward -n ingress-nginx service/ingress-nginx-controller 8080:80

No navegador:

- Swagger do ms-habit:   http://localhost:8080/swagger-ui/index.html
- Swagger do ms-produto: http://localhost:8080/produto/swagger-ui/index.html
- Qual pod respondeu:    http://localhost:8080/instance

## Decisões de projeto

**Porta 8081.** O `ms-habit` usa `server.port=8081`, então `containerPort`,
`targetPort` e o `port` do Service são 8081 — diferente do ms-produto, que usa 8080.

**Context path no ms-produto.** Os dois microsserviços servem o Swagger em
`/swagger-ui/*`. Para que convivam sob um único Ingress sem colisão, o ms-produto
recebe `SERVER_SERVLET_CONTEXT_PATH=/produto` e o Ingress roteia `/produto` para ele
e `/` para o ms-habit. O ms-habit ficou na raiz de propósito: é ele que vale a nota.

**readinessProbe.** Sem ela o Kubernetes marca o pod como pronto assim que o
contêiner inicia, mas a JVM ainda está subindo — e o Service manda tráfego para uma
aplicação que ainda não escuta. Com 5 réplicas isso apareceria como requisição
falhando durante a apresentação.

**Sem PersistentVolume.** Os bancos usam armazenamento efêmero: se o pod do MySQL
for recriado, os dados somem. Suficiente para a avaliação, inadequado para produção.
