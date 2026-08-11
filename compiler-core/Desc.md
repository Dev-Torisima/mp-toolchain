## 関数呼び出し規約（アセンブリ）  

引数は Windows32 ABI と同じ  
> 右から左に向けてpushして、popが引数の順になるよう  
callerが呼出し後、pushした分だけpopをする  
rbpレジスタは callee-save  
他のレジスタは caller-save  
raxレジスタは返り値  
